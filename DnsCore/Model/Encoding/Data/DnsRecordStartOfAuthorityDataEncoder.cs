using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordStartOfAuthorityDataEncoder : DnsRecordDataEncoder<DnsStartOfAuthorityRecordData>
{
    public static readonly DnsRecordStartOfAuthorityDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsStartOfAuthorityRecordData data)
    {
        DnsNameEncoder.Encode(ref writer, data.PrimaryNameServer);
        DnsNameEncoder.Encode(ref writer, data.ResponsibleMailbox);
        writer.Write(data.Serial);
        writer.WriteTime(data.Refresh);
        writer.WriteTime(data.Retry);
        writer.WriteTime(data.Expire);
        writer.WriteTime(data.Minimum);
    }

    protected override DnsStartOfAuthorityRecordData DecodeData(ref DnsReader reader)
    {
        var primaryNameServer = DnsNameEncoder.Decode(ref reader);
        var responsibleMailbox = DnsNameEncoder.Decode(ref reader);
        var serial = reader.Read<uint>();
        var refresh = reader.ReadTime();
        var retry = reader.ReadTime();
        var expire = reader.ReadTime();
        var minimum = reader.ReadTime();
        return new(primaryNameServer, responsibleMailbox, serial, refresh, retry, expire, minimum);
    }

    protected override DnsRecord<DnsStartOfAuthorityRecordData> CreateRecord(
        DnsName name,
        DnsStartOfAuthorityRecordData data,
        DnsRecordType recordType,
        DnsClass @class,
        TimeSpan ttl)
    {
        return new DnsStartOfAuthorityRecord(
            name,
            data.PrimaryNameServer,
            data.ResponsibleMailbox,
            data.Serial,
            data.Refresh,
            data.Retry,
            data.Expire,
            data.Minimum,
            ttl);
    }
}