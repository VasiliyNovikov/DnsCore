using System;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordNameServerDataEncoder : DnsRecordNameDataEncoder
{
    public static readonly DnsRecordNameServerDataEncoder Instance = new();

    protected override DnsRecord<DnsName> CreateRecord(DnsName name, DnsName data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsNameServerRecord(name, data, ttl);
}