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
        writer.Write((uint)data.Refresh.TotalSeconds);
        writer.Write((uint)data.Retry.TotalSeconds);
        writer.Write((uint)data.Expire.TotalSeconds);
        writer.Write((uint)data.Minimum.TotalSeconds);
    }

    protected override DnsStartOfAuthorityRecordData DecodeData(ref DnsReader reader)
    {
        var primaryNameServer = DnsNameEncoder.Decode(ref reader);
        var responsibleMailbox = DnsNameEncoder.Decode(ref reader);
        var serial = reader.Read<uint>();
        var refresh = TimeSpan.FromSeconds(reader.Read<uint>());
        var retry = TimeSpan.FromSeconds(reader.Read<uint>());
        var expire = TimeSpan.FromSeconds(reader.Read<uint>());
        var minimum = TimeSpan.FromSeconds(reader.Read<uint>());
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