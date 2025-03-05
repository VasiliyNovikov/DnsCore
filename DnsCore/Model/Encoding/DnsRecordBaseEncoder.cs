using DnsCore.IO;

namespace DnsCore.Model.Encoding;

internal static class DnsRecordBaseEncoder
{
    public static void Encode(ref DnsWriter writer, DnsRecordBase record)
    {
        DnsNameEncoder.Encode(ref writer, record.Name);
        writer.Write((ushort)record.RecordType);
        writer.Write((ushort)record.Class);
    }

    public static (DnsName Name, DnsRecordType RecordType, DnsClass Class) Decode(ref DnsReader reader)
    {
        var name = DnsNameEncoder.Decode(ref reader);
        var type = (DnsRecordType)reader.Read<ushort>();
        var @class = (DnsClass)reader.Read<ushort>();
        return (name, type, @class);
    }
}