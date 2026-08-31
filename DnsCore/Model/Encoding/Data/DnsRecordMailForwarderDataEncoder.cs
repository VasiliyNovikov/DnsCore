using System;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordMailForwarderDataEncoder : DnsRecordNameDataEncoder
{
    public static readonly DnsRecordMailForwarderDataEncoder Instance = new();

    protected override DnsRecord<DnsName> CreateRecord(DnsName name, DnsName data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsMailForwarderRecord(name, data, ttl);
}