using System;
using System.Collections.Generic;

using DnsCore.Model.Internal;

namespace DnsCore.Model;

public sealed class DnsRequest : DnsMessage
{
    private DnsRequest(DnsRawMessage rawMessage)
        : base(rawMessage)
    {
    }

    public DnsRequest(ushort id, IEnumerable<DnsQuestion>? questions = null)
        : base(id, questions)
    {
    }

    public DnsRequest(IEnumerable<DnsQuestion>? questions = null)
        : base(DnsMessageIdGenerator.NextId(), questions)
    {
    }

    public DnsRequest(DnsName name, DnsRecordType recordType)
        : this([new(name, recordType)])
    {
    }

    public DnsResponse Reply() => new(this);

    public DnsResponse Reply(DnsResponseStatus status)
    {
        var response = Reply();
        response.Status = status;
        return response;
    }

    public DnsResponse Reply(DnsRecord answer)
    {
        var response = Reply();
        response.Answers.Add(answer);
        return response;
    }

    public DnsResponse Reply(params DnsRecord[] answers)
    {
        var response = Reply();
        response.Answers.AddRange(answers);
        return response;
    }

    public static DnsRequest Decode(ReadOnlySpan<byte> buffer) => new(DnsRawMessage.Decode(buffer));

    private protected override DnsRawMessage ToRawMessage() => new(Id, GetRawFlags(), Questions, [], [], []);
}