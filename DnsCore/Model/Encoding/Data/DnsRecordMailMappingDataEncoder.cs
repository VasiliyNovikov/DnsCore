using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordMailMappingDataEncoder : DnsRecordDataEncoder<DnsMailMappingRecordData>
{
    public static readonly DnsRecordMailMappingDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsMailMappingRecordData data)
    {
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
        if (@class != DnsClass.IN)
            throw new FormatException("PX record data is only defined for the IN class");
        return new DnsMailMappingRecord(name, data.Preference, data.Map822, data.MapX400, ttl);
    }

    protected override void ValidateData(DnsMailMappingRecordData data)
    {
        ArgumentNullException.ThrowIfNull(data.Map822);
        ArgumentNullException.ThrowIfNull(data.MapX400);
        ValidateDataLength(checked(2 + GetUncompressedLength(data.Map822) + GetUncompressedLength(data.MapX400)));
    }
}