using System;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordPtrDataEncoder : DnsRecordNameDataEncoder
{
    public static readonly DnsRecordPtrDataEncoder Instance = new();

    protected override DnsRecord<DnsName> CreateRecord(DnsName name, DnsName data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsPtrRecord(name, data, ttl);
}