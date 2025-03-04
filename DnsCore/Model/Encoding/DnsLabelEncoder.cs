using DnsCore.IO;

namespace DnsCore.Model.Encoding;

internal static class DnsLabelEncoder
{
    private static readonly System.Text.Encoding Encoding = System.Text.Encoding.ASCII;

    public static void Encode(ref DnsWriter writer, DnsLabel label)
    {
        writer.Write((byte)label.Length);
        if (label.IsEmpty)
            return;
        Encoding.GetBytes(label.Span, writer.ProvideBufferAndAdvance(label.Length));
    }

    public static DnsLabel Decode(ref DnsReader reader)
    {
        var length = reader.Read<byte>();
        if (length == 0)
            return DnsLabel.Empty;

        var labelStr = Encoding.GetString(reader.Read(length));
        DnsLabel.Validate(labelStr);
        return new DnsLabel(labelStr);
    }
}