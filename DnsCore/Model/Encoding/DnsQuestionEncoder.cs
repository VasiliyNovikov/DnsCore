using DnsCore.IO;

namespace DnsCore.Model.Encoding;

internal static class DnsQuestionEncoder
{
    public static void Encode(ref DnsWriter writer, DnsQuestion question) => DnsRecordBaseEncoder.Encode(ref writer, question);

    public static DnsQuestion Decode(ref DnsReader reader)
    {
        var (name, type, @class) = DnsRecordBaseEncoder.Decode(ref reader);
        return new DnsQuestion(name, type, @class);
    }
}