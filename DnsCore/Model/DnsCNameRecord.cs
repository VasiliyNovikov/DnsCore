using System;

namespace DnsCore.Model;

public sealed class DnsCNameRecord(DnsName name, DnsName data, TimeSpan ttl) : DnsNameRecord(name, data, DnsRecordType.CNAME, ttl);