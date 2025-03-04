namespace DnsCore.Model;

public abstract class DnsRecordBase(DnsName name, DnsRecordType recordType, DnsClass @class)
{
    public DnsName Name => name;
    public DnsRecordType RecordType => recordType;
    public DnsClass Class => @class;

    public override string ToString() => $"{name,-40} {@class,-4} {recordType,-6}";
}
