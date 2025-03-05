using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding;

internal static class DnsNameEncoder
{
    private const byte CompressionMask = 0b1100_0000;
    private const ushort OffsetMask = CompressionMask << 8;
    private const ushort OffsetMaskInverted = unchecked((ushort)~OffsetMask);

    public static void Encode(ref DnsWriter writer, DnsName name)
    {
        if (!name.IsEmpty)
        {
            if (writer.GetNameOffset(name, out var offset))
            {
                writer.Write((ushort)(offset | OffsetMask));
                return;
            }
            writer.AddNameOffset(name, writer.Position);
        }

        DnsLabelEncoder.Encode(ref writer, name.Label);
        if (name.Parent is { } parent)
            Encode(ref writer, parent);
    }

    public static DnsName Decode(ref DnsReader reader) => DecodeInternal(ref reader);

    private static DnsName DecodeInternal(ref DnsReader reader, int maxLength = DnsName.MaxLength, bool canStartWithCompression = true)
    {
        if (maxLength <= 0)
            throw new FormatException("DNS name too long");

        if (reader.Peek<byte>() >= CompressionMask)
        {
            if (!canStartWithCompression)
                throw new FormatException("DNS name compression pointer can't point to another pointer");

            var offset = (ushort)(reader.Read<ushort>() & OffsetMaskInverted);
            if (!reader.GetNameByOffset(offset, out var name))
            {
                var offsetReader = reader.GetSubReader(offset);
                name = DecodeInternal(ref offsetReader, maxLength, false);
                reader.AddNameOffset(offset, name);
            }
            return name;
        }
        else
        {
            var offset = reader.Position;
            var label = DnsLabelEncoder.Decode(ref reader);
            if (label.IsEmpty)
                return DnsName.Empty;

            var parent = DecodeInternal(ref reader, maxLength - label.Length - 1);
            var name = new DnsName(label, parent);
            reader.AddNameOffset(offset, name);
            return name;
        }
    }
}