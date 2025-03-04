using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using DnsCore.Model.Internal;

namespace DnsCore.Model;

public abstract class DnsMessage
{
    public ushort Id { get; }
    public DnsRequestType RequestType { get; set; }
    public bool RecursionDesired { get; set; }

    public List<DnsQuestion> Questions { get; } = new(1);
    public List<DnsRecord> Additional { get; } = new();

    private protected DnsMessage(DnsRawMessage rawMessage)
        : this(rawMessage.Id, rawMessage.Questions, rawMessage.Additional)
    {
        RequestType = (rawMessage.Flags & DnsFlags.OpCodeMask) switch
        {
            DnsFlags.OpCodeIQuery => DnsRequestType.InverseQuery,
            DnsFlags.OpCodeStatus => DnsRequestType.Status,
            _ => DnsRequestType.Query
        };
        RecursionDesired = (rawMessage.Flags & DnsFlags.RecursionDesired) == DnsFlags.RecursionDesired;
    }

    protected DnsMessage(ushort id, IEnumerable<DnsQuestion>? questions = null, IEnumerable<DnsRecord>? additional = null)
    {
        Id = id;
        if (questions is not null)
            Questions.AddRange(questions);
        if (additional is not null)
            Additional.AddRange(additional);
    }

    private protected virtual void FormatHeader(StringBuilder target)
    {
        target.AppendLine(CultureInfo.InvariantCulture, $"ID:    {Id}");
        target.AppendLine(CultureInfo.InvariantCulture, $"Type:  {RequestType}");
        target.AppendLine(CultureInfo.InvariantCulture, $"RD:    {RecursionDesired}");
    }

    private protected virtual void FormatBody(StringBuilder target)
    {
        target.AppendLine("Questions:");
        foreach (var question in Questions)
            target.AppendLine(CultureInfo.InvariantCulture, $"    {question}");
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        FormatHeader(result);
        FormatBody(result);
        return result.ToString();
    }

    internal abstract DnsRawMessage ToRawMessage();

    private protected virtual DnsFlags GetRawFlags()
    {
        var flags = RequestType switch
        {
            DnsRequestType.Query => DnsFlags.OpCodeQuery,
            DnsRequestType.InverseQuery => DnsFlags.OpCodeIQuery,
            DnsRequestType.Status => DnsFlags.OpCodeStatus,
            _ => throw new ArgumentOutOfRangeException(nameof(RequestType))
        };
        if (RecursionDesired)
            flags |= DnsFlags.RecursionDesired;
        return flags;
    }
}