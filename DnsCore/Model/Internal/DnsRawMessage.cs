namespace DnsCore.Model.Internal;

internal sealed class DnsRawMessage(ushort id, DnsFlags flags, DnsQuestion[] questions, DnsRecord[] answers, DnsRecord[] authorities, DnsRecord[] additional)
{
    public ushort Id => id;
    public DnsFlags Flags => flags;
    public DnsQuestion[] Questions => questions;
    public DnsRecord[] Answers => answers;
    public DnsRecord[] Authorities => authorities;
    public DnsRecord[] Additional => additional;
}