using System;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordMailDestinationDataEncoder : DnsRecordNameDataEncoder
{
    public static readonly DnsRecordMailDestinationDataEncoder Instance = new();

    protected override DnsRecord<DnsName> CreateRecord(DnsName name, DnsName data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsMailDestinationRecord(name, data, ttl);
}