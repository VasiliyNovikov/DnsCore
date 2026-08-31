using System;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordMailRenameDataEncoder : DnsRecordNameDataEncoder
{
    public static readonly DnsRecordMailRenameDataEncoder Instance = new();

    protected override DnsRecord<DnsName> CreateRecord(DnsName name, DnsName data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsMailRenameRecord(name, data, ttl);
}