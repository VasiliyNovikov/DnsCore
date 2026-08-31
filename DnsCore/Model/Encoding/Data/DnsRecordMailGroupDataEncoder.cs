using System;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordMailGroupDataEncoder : DnsRecordNameDataEncoder
{
    public static readonly DnsRecordMailGroupDataEncoder Instance = new();

    protected override DnsRecord<DnsName> CreateRecord(DnsName name, DnsName data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsMailGroupRecord(name, data, ttl);
}