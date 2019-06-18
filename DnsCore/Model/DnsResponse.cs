using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using DnsCore.Model.Internal;

namespace DnsCore.Model;

public sealed class DnsResponse : DnsMessage
{
    public bool RecursionAvailable { get; set; }
    public bool AuthoritativeAnswer { get; set; }
    public DnsResponseStatus Status { get; set; }
    public List<DnsRecord> Answers { get; } = new(1);
    public List<DnsRecord> Authorities { get; } = new();
    public List<DnsRecord> Additional { get; } = new();

    private DnsResponse(DnsRawMessage rawMessage)
        : base(rawMessage)
    {
        RecursionAvailable = (rawMessage.Flags & DnsFlags.RecursionAvailable) == DnsFlags.RecursionAvailable;
        AuthoritativeAnswer = (rawMessage.Flags & DnsFlags.AuthoritativeAnswer) == DnsFlags.AuthoritativeAnswer;
        Status = (rawMessage.Flags & DnsFlags.ResponseCodeMask) switch
        {
            DnsFlags.FormatError => DnsResponseStatus.FormatError,
            DnsFlags.ServerFailure => DnsResponseStatus.ServerFailure,
            DnsFlags.NameError => DnsResponseStatus.NameError,
            DnsFlags.NotImplemented => DnsResponseStatus.NotImplemented,
            DnsFlags.Refused => DnsResponseStatus.Refused,
            _ => DnsResponseStatus.Ok
        };
        Answers.AddRange(rawMessage.Answers);
        Authorities.AddRange(rawMessage.Authorities);
        Additional.AddRange(rawMessage.Additional);
    }

    public DnsResponse(ushort id, IEnumerable<DnsQuestion>? questions = null, IEnumerable<DnsRecord>? answers = null) : base(id, questions)
    {
        if (answers is not null)
            Answers.AddRange(answers);
    }

    public DnsResponse(DnsRequest request, IEnumerable<DnsRecord>? answers = null)
        : this((request ?? throw new ArgumentNullException(nameof(request))).Id, request.Questions, answers)
    {
        RequestType = request.RequestType;
        RecursionDesired = request.RecursionDesired;
    }

    private protected override void FormatHeader(StringBuilder target)
    {
        base.FormatHeader(target);
        target.AppendLine(CultureInfo.InvariantCulture, $"RA:    {RecursionAvailable}");
        target.AppendLine(CultureInfo.InvariantCulture, $"AA:    {AuthoritativeAnswer}");
        target.AppendLine(CultureInfo.InvariantCulture, $"RCode: {Status}");
    }

    private protected override void FormatBody(StringBuilder target)
    {
        base.FormatBody(target);
        if (Answers.Count != 0)
        {
            target.AppendLine("Answers:");
            foreach (var answer in Answers)
                target.AppendLine(CultureInfo.InvariantCulture, $"    {answer}");
        }
        if (Authorities.Count != 0)
        {
            target.AppendLine("Authorities:");
            foreach (var authority in Authorities)
                target.AppendLine(CultureInfo.InvariantCulture, $"    {authority}");
        }
        if (Additional.Count != 0)
        {
            target.AppendLine("Additional:");
            foreach (var additional in Additional)
                target.AppendLine(CultureInfo.InvariantCulture, $"    {additional}");
        }
    }

    public static DnsResponse Decode(ReadOnlySpan<byte> buffer) => new(DnsRawMessage.Decode(buffer));

    private protected override DnsRawMessage ToRawMessage() => new(Id, GetRawFlags(), Questions, Answers, Authorities, Additional);

    private protected override DnsFlags GetRawFlags()
    {
        var flags = base.GetRawFlags() | DnsFlags.Response;
        if (RecursionAvailable)
            flags |= DnsFlags.RecursionAvailable;
        if (AuthoritativeAnswer)
            flags |= DnsFlags.AuthoritativeAnswer;
        flags |= Status switch
        {
            DnsResponseStatus.Ok => DnsFlags.NoError,
            DnsResponseStatus.FormatError => DnsFlags.FormatError,
            DnsResponseStatus.ServerFailure => DnsFlags.ServerFailure,
            DnsResponseStatus.NameError => DnsFlags.NameError,
            DnsResponseStatus.NotImplemented => DnsFlags.NotImplemented,
            DnsResponseStatus.Refused => DnsFlags.Refused,
            _ => throw new ArgumentOutOfRangeException(nameof(Status))
        };
        return flags;
    }
}