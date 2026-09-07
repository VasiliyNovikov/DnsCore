namespace DnsCore.Model;

public readonly record struct DnsResponsiblePersonRecordData(DnsName Mailbox, DnsName TextName)
{
    public override string ToString() => $"{Mailbox} {TextName}";
}