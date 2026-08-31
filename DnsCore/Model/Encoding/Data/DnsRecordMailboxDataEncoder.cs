using System;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordMailboxDataEncoder : DnsRecordNameDataEncoder
{
    public static readonly DnsRecordMailboxDataEncoder Instance = new();

    protected override DnsRecord<DnsName> CreateRecord(DnsName name, DnsName data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsMailboxRecord(name, data, ttl);
}