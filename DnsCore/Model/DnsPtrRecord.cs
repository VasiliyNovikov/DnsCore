using System;

namespace DnsCore.Model;

public sealed class DnsPtrRecord(DnsName name, DnsName data, TimeSpan ttl) : DnsNameRecord(name, data, DnsRecordType.PTR, ttl);