using System;

namespace DnsCore.Model;

public sealed class DnsMailboxRecord(DnsName name, DnsName data, TimeSpan ttl) : DnsNameRecord(name, data, DnsRecordType.MB, ttl);