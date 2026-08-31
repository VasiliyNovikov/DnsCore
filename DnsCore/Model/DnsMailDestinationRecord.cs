using System;

namespace DnsCore.Model;

public sealed class DnsMailDestinationRecord(DnsName name, DnsName data, TimeSpan ttl) : DnsNameRecord(name, data, DnsRecordType.MD, ttl);