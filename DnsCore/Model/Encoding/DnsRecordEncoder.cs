using System;
using System.Buffers.Binary;

using DnsCore.IO;
using DnsCore.Model.Encoding.Data;

namespace DnsCore.Model.Encoding;

internal static class DnsRecordEncoder
{
    private static readonly DnsRecordDataEncoder DefaultEncoder;
    private static readonly DnsRecordDataEncoder?[] Encoders;

    static DnsRecordEncoder()
    {
        DefaultEncoder = DnsRecordRawDataEncoder.Instance;
        Encoders = new DnsRecordDataEncoder?[UInt16.MaxValue + 1];
        RegisterTypeEncoder(DnsRecordType.A, DnsRecordAddressDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.AAAA, DnsRecordAddressDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.CNAME, DnsRecordCNameDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.PTR, DnsRecordPtrDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.TXT, DnsRecordTextDataEncoder.Instance);
    }

    public static void RegisterTypeEncoder(DnsRecordType type, DnsRecordDataEncoder encoder) => Encoders[(ushort)type] = encoder;

    private static DnsRecordDataEncoder GetEncoder(DnsRecordType type) => Encoders[(ushort)type] ?? DefaultEncoder;

    public static void Encode(ref DnsWriter writer, DnsRecord record)
    {
        DnsRecordBaseEncoder.Encode(ref writer, record);
        writer.Write((uint)record.Ttl.TotalSeconds);

        var dataLenBuffer = writer.ProvideBufferAndAdvance(2);

        var dataPosition = writer.Position;
        GetEncoder(record.RecordType).Encode(ref writer, record);

        BinaryPrimitives.WriteUInt16BigEndian(dataLenBuffer, (ushort)(writer.Position - dataPosition));
    }

    public static DnsRecord Decode(ref DnsReader reader)
    {
        var (name, type, @class) = DnsRecordBaseEncoder.Decode(ref reader);
        var ttl = TimeSpan.FromSeconds(reader.Read<uint>());
        var dataLength = reader.Read<ushort>();
        var dataReader = reader.GetSubReader(reader.Position, dataLength);
        reader.Skip(dataLength);
        return GetEncoder(type).Decode(ref dataReader, name, type, @class, ttl);
    }
}