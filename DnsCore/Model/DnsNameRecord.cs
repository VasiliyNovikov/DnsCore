using System;

using DnsCore.IO;

namespace DnsCore.Model;

public abstract class DnsNameRecord(DnsName name, DnsName data, DnsRecordType recordType, TimeSpan ttl)
    : DnsRecord<DnsName>(name, data, recordType, DnsClass.IN, ttl)
{
    private protected override void EncodeData(ref DnsWriter writer) => Data.Encode(ref writer);
    internal static DnsName DecodeData(ref DnsReader reader) => DnsName.Decode(ref reader);
}