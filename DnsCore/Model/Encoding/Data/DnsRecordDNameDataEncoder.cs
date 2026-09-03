using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordDNameDataEncoder : DnsRecordNameDataEncoder
{
    public static readonly DnsRecordDNameDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsName data) => DnsNameEncoder.Encode(ref writer, data, false);
    protected override DnsRecord<DnsName> CreateRecord(DnsName name, DnsName data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsDNameRecord(name, data, @class, ttl);
}