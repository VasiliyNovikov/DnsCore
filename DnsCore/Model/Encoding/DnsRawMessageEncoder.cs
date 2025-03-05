using System;

using DnsCore.IO;
using DnsCore.Model.Internal;

namespace DnsCore.Model.Encoding;

internal static class DnsRawMessageEncoder
{
    public static ushort Encode(Span<byte> buffer, DnsRawMessage message)
    {
        var writer = new DnsWriter(buffer);
        Encode(ref writer, message);
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
        catch (OverflowException e)
        {
            throw new FormatException($"Invalid DNS message: {e.Message}", e);
        }
        catch (ArgumentException e)
        {
            throw new FormatException($"Invalid DNS message: {e.Message}", e);
        }
    }

    private static void Encode(ref DnsWriter writer, DnsRawMessage message)
    {
        try
        {
            writer.Write(message.Id);
            writer.Write((ushort)message.Flags);
            writer.Write((ushort)message.Questions.Length);
            writer.Write((ushort)message.Answers.Length);
            writer.Write((ushort)message.Authorities.Length);
            writer.Write((ushort)message.Additional.Length);
            foreach (var question in message.Questions)
                DnsQuestionEncoder.Encode(ref writer, question);
            foreach (var answer in message.Answers)
                DnsRecordEncoder.Encode(ref writer, answer);
            foreach (var authority in message.Authorities)
                DnsRecordEncoder.Encode(ref writer, authority);
            foreach (var additional in message.Additional)
                DnsRecordEncoder.Encode(ref writer, additional);
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
            questions[i] = DnsQuestionEncoder.Decode(ref reader);

        var answers = new DnsRecord[answerCount];
        for (var i = 0; i < answerCount; ++i)
            answers[i] = DnsRecordEncoder.Decode(ref reader);

        var authorities = new DnsRecord[authorityCount];
        for (var i = 0; i < authorityCount; ++i)
            authorities[i] = DnsRecordEncoder.Decode(ref reader);

        var additional = new DnsRecord[additionalCount];
        for (var i = 0; i < additionalCount; ++i)
            additional[i] = DnsRecordEncoder.Decode(ref reader);

        return new DnsRawMessage(id, flags, questions, answers, authorities, additional);
    }
}