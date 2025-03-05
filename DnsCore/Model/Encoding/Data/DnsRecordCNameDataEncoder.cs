using System;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordCNameDataEncoder : DnsRecordNameDataEncoder
{
    public static readonly DnsRecordCNameDataEncoder Instance = new();

    protected override DnsRecord<DnsName> CreateRecord(DnsName name, DnsName data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsCNameRecord(name, data, ttl);
}