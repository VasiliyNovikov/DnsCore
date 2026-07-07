namespace DnsCore.Model;

public readonly record struct DnsServiceRecordData(ushort Priority, ushort Weight, ushort Port, DnsName Target)
{
    public override string ToString() => $"{Priority} {Weight} {Port} {Target}";
}