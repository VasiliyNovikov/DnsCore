using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordRouteThroughDataEncoder : DnsRecordDataEncoder<DnsRouteThroughRecordData>
{
    public static readonly DnsRecordRouteThroughDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsRouteThroughRecordData data)
    {
        writer.Write(data.Preference);
        DnsNameEncoder.Encode(ref writer, data.IntermediateHost, false);
    }

    protected override DnsRouteThroughRecordData DecodeData(ref DnsReader reader)
    {
        return new(reader.Read<ushort>(), DnsNameEncoder.Decode(ref reader));
    }

    protected override DnsRecord<DnsRouteThroughRecordData> CreateRecord(DnsName name, DnsRouteThroughRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsRouteThroughRecord(name, data.Preference, data.IntermediateHost, @class, ttl);
    }

    protected override void ValidateData(DnsRouteThroughRecordData data)
    {
        ArgumentNullException.ThrowIfNull(data.IntermediateHost);
        ValidateDataLength(checked(2 + GetUncompressedLength(data.IntermediateHost)));
    }
}