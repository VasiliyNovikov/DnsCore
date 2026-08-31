using System;

namespace DnsCore.Model;

public sealed class DnsMailInformationRecord(DnsName name, DnsName responsibleMailbox, DnsName errorMailbox, TimeSpan ttl)
    : DnsRecord<DnsMailInformationRecordData>(name, new(responsibleMailbox, errorMailbox), DnsRecordType.MINFO, DnsClass.IN, ttl);