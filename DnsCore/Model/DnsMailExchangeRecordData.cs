namespace DnsCore.Model;

public readonly record struct DnsMailExchangeRecordData(ushort Preference, DnsName Exchange)
{
    public override string ToString() => $"{Preference} {Exchange}";
}