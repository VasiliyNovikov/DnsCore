using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordAfsDatabaseDataEncoder : DnsRecordDataEncoder<DnsAfsDatabaseRecordData>
{
    public static readonly DnsRecordAfsDatabaseDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsAfsDatabaseRecordData data)
    {
        writer.Write(data.Subtype);
        DnsNameEncoder.Encode(ref writer, data.Hostname, false);
    }

    protected override DnsAfsDatabaseRecordData DecodeData(ref DnsReader reader)
    {
        return new(reader.Read<ushort>(), DnsNameEncoder.Decode(ref reader));
    }

    protected override DnsRecord<DnsAfsDatabaseRecordData> CreateRecord(DnsName name, DnsAfsDatabaseRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsAfsDatabaseRecord(name, data.Subtype, data.Hostname, @class, ttl);
    }

    protected override void ValidateData(DnsAfsDatabaseRecordData data)
    {
        ArgumentNullException.ThrowIfNull(data.Hostname);
        ValidateDataLength(checked(2 + GetUncompressedLength(data.Hostname)));
    }
}