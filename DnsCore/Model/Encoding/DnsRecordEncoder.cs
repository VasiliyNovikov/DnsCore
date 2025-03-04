using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;

using DnsCore.IO;
using DnsCore.Model.Encoding.Data;

namespace DnsCore.Model.Encoding;

internal static class DnsRecordEncoder
{
    private static readonly DnsRecordDataEncoder DefaultEncoder;
    private static readonly ConcurrentDictionary<DnsRecordType, DnsRecordDataEncoder> Encoders;

    static DnsRecordEncoder()
    {
        DefaultEncoder = DnsRecordRawDataEncoder.Instance;
        Encoders = new();
        RegisterTypeEncoder(DnsRecordType.A, DnsRecordAddressDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.AAAA, DnsRecordAddressDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.CNAME, DnsRecordCNameDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.PTR, DnsRecordPtrDataEncoder.Instance);
    }

    public static void RegisterTypeEncoder(DnsRecordType type, DnsRecordDataEncoder encoder) => Encoders[type] = encoder;

    private static DnsRecordDataEncoder GetEncoder(DnsRecordType type) => Encoders.GetValueOrDefault(type, DefaultEncoder);

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