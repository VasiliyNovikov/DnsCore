using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordMailMappingDataEncoder : DnsRecordDataEncoder<DnsMailMappingRecordData>
{
    public static readonly DnsRecordMailMappingDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsMailMappingRecordData data)
    {
        ArgumentNullException.ThrowIfNull(data.Map822);
        ArgumentNullException.ThrowIfNull(data.MapX400);
        writer.Write(data.Preference);
        DnsNameEncoder.Encode(ref writer, data.Map822, false);
        DnsNameEncoder.Encode(ref writer, data.MapX400, false);
    }

    protected override DnsMailMappingRecordData DecodeData(ref DnsReader reader)
    {
        return new(reader.Read<ushort>(), DnsNameEncoder.Decode(ref reader), DnsNameEncoder.Decode(ref reader));
    }

    protected override DnsRecord<DnsMailMappingRecordData> CreateRecord(DnsName name, DnsMailMappingRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsMailMappingRecord(name, data.Preference, data.Map822, data.MapX400, ttl);
    }
}