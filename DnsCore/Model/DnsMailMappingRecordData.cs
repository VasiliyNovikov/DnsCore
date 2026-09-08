namespace DnsCore.Model;

public readonly record struct DnsMailMappingRecordData(ushort Preference, DnsName Map822, DnsName MapX400)
{
    public override string ToString() => $"{Preference} {Map822} {MapX400}";
}