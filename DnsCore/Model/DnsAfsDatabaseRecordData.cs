namespace DnsCore.Model;

public readonly record struct DnsAfsDatabaseRecordData(ushort Subtype, DnsName Hostname)
{
    public override string ToString() => $"{Subtype} {Hostname}";
}