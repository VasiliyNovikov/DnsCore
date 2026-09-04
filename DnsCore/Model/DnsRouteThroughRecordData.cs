namespace DnsCore.Model;

public readonly record struct DnsRouteThroughRecordData(ushort Preference, DnsName IntermediateHost)
{
    public override string ToString() => $"{Preference} {IntermediateHost}";
}