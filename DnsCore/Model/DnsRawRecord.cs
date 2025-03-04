using System;

namespace DnsCore.Model;

public sealed class DnsRawRecord(DnsName name, byte[] data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    : DnsRecord<byte[]>(name, data, recordType, @class, ttl)
{
    private protected override string DataToString() => BitConverter.ToString(Data);
}