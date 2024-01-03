using System;

using DnsCore.Encoding;

namespace DnsCore.Model.Internal;

internal sealed class DnsRawMessage(ushort id, DnsFlags flags, DnsQuestion[] questions, DnsRecord[] answers, DnsRecord[] authorities, DnsRecord[] additional)
{
    public ushort Id => id;
    public DnsFlags Flags => flags;
    public DnsQuestion[] Questions => questions;
    public DnsRecord[] Answers => answers;
    public DnsRecord[] Authorities => authorities;
    public DnsRecord[] Additional => additional;

    public int Encode(Span<byte> buffer)
    {
        var writer = new DnsWriter(buffer);
        Encode(ref writer);
        return writer.Position;
    }

    public static DnsRawMessage Decode(ReadOnlySpan<byte> buffer)
    {
        var reader = new DnsReader(buffer);
        try
        {
            var result = Decode(ref reader);
            return reader.Position == buffer.Length
                ? result
                : throw new FormatException("Invalid DNS message: buffer contains extra data");
        }
        catch (ArgumentOutOfRangeException e)
        {
            throw new FormatException("Invalid DNS message: buffer is too short", e);
        }
        catch (ArgumentException e)
        {
            throw new FormatException($"Invalid DNS message: {e.Message}", e);
        }
    }

    private void Encode(ref DnsWriter writer)
    {
        try
        {
            writer.Write(Id);
            writer.Write((ushort)Flags);
            writer.Write((ushort)Questions.Length);
            writer.Write((ushort)Answers.Length);
            writer.Write((ushort)Authorities.Length);
            writer.Write((ushort)Additional.Length);
            foreach (var question in Questions)
                question.Encode(ref writer);
            foreach (var answer in Answers)
                answer.Encode(ref writer);
            foreach (var authority in Authorities)
                authority.Encode(ref writer);
            foreach (var additional in Additional)
                additional.Encode(ref writer);
        }
        catch (ArgumentException e)
        {
            throw new FormatException($"Buffer is too short: {e.Message}", e);
        }
    }

    private static DnsRawMessage Decode(ref DnsReader reader)
    {
        var id = reader.Read<ushort>();
        var flags = (DnsFlags)reader.Read<ushort>();
        var questionCount = reader.Read<ushort>();
        var answerCount = reader.Read<ushort>();
        var authorityCount = reader.Read<ushort>();
        var additionalCount = reader.Read<ushort>();

        var questions = new DnsQuestion[questionCount];
        for (var i = 0; i < questionCount; ++i)
            questions[i] = DnsQuestion.Decode(ref reader);

        var answers = new DnsRecord[answerCount];
        for (var i = 0; i < answerCount; ++i)
            answers[i] = DnsRecord.Decode(ref reader);

        var authorities = new DnsRecord[authorityCount];
        for (var i = 0; i < authorityCount; ++i)
            authorities[i] = DnsRecord.Decode(ref reader);

        var additional = new DnsRecord[additionalCount];
        for (var i = 0; i < additionalCount; ++i)
            additional[i] = DnsRecord.Decode(ref reader);

        return new DnsRawMessage(id, flags, questions, answers, authorities, additional);
    }
}