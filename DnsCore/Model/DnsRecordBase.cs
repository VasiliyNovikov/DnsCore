using DnsCore.Encoding;

namespace DnsCore.Model;

public abstract class DnsRecordBase(DnsName name, DnsRecordType recordType, DnsClass @class)
{
    public DnsName Name => name;
    public DnsRecordType RecordType => recordType;
    public DnsClass Class => @class;

    public override string ToString() => $"{name,-32} {@class,-4} {recordType,-6}";

    internal virtual void Encode(ref DnsWriter writer)
    {
        name.Encode(ref writer);
        writer.Write((ushort)recordType);
        writer.Write((ushort)@class);
    }

    internal static (DnsName Name, DnsRecordType RecordType, DnsClass Class) Decode(ref DnsReader reader)
    {
        var name = DnsName.Decode(ref reader);
        var type = (DnsRecordType)reader.Read<ushort>();
        var @class = (DnsClass)reader.Read<ushort>();
        return (name, type, @class);
    }
}
