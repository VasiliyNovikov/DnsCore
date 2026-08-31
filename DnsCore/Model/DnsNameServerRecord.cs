using System;

namespace DnsCore.Model;

public sealed class DnsNameServerRecord(DnsName name, DnsName data, TimeSpan ttl) : DnsNameRecord(name, data, DnsRecordType.NS, ttl);