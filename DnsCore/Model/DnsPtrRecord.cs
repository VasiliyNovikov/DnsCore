using System;

namespace DnsCore.Model;

public sealed class DnsPtrRecord(DnsName name, DnsName ptrName, TimeSpan ttl)
    : DnsNameRecord(name, DnsRecordType.PTR, ptrName, ttl)
{
    public DnsName PtrName => AnswerName;
}