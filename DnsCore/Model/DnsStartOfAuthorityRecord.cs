using System;

namespace DnsCore.Model;

public sealed class DnsStartOfAuthorityRecord(DnsName name, DnsName primaryNameServer, DnsName responsibleMailbox, uint serial, TimeSpan refresh, TimeSpan retry, TimeSpan expire, TimeSpan minimum, TimeSpan ttl)
    : DnsRecord<DnsStartOfAuthorityRecordData>(name, new(primaryNameServer, responsibleMailbox, serial, refresh, retry, expire, minimum), DnsRecordType.SOA, DnsClass.IN, ttl);