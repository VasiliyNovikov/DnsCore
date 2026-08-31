namespace DnsCore.Model;

public readonly record struct DnsMailInformationRecordData(DnsName ResponsibleMailbox, DnsName ErrorMailbox)
{
    public override string ToString() => $"{ResponsibleMailbox} {ErrorMailbox}";
}