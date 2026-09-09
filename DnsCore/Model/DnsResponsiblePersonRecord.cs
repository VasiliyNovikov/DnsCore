using System;

namespace DnsCore.Model;

public sealed class DnsResponsiblePersonRecord : DnsRecord<DnsResponsiblePersonRecordData>
{
    public DnsResponsiblePersonRecord(DnsName name, DnsName mailbox, DnsName textName, TimeSpan ttl)
        : this(name, mailbox, textName, DnsClass.IN, ttl) { }

    internal DnsResponsiblePersonRecord(DnsName name, DnsName mailbox, DnsName textName, DnsClass @class, TimeSpan ttl)
        : base(name, new(mailbox, textName), DnsRecordType.RP, @class, ttl) { }
}