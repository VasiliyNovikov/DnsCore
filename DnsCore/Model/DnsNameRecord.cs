using System;

using DnsCore.Encoding;

namespace DnsCore.Model;

public abstract  class DnsNameRecord(DnsName name, DnsRecordType recordType, DnsName answerName, TimeSpan ttl)
    : DnsRecord(name, recordType, DnsClass.IN, ttl)
{
    protected DnsName AnswerName { get; } = answerName;
    public override string ToString() => $"{base.ToString()} {AnswerName}";
    private protected override void EncodeData(ref DnsWriter writer) => AnswerName.Encode(ref writer);
    internal static DnsName DecodeData(ref DnsReader reader) => DnsName.Decode(ref reader);
}