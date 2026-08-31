using System;

namespace DnsCore.Model;

public sealed class DnsMailForwarderRecord(DnsName name, DnsName data, TimeSpan ttl) : DnsNameRecord(name, data, DnsRecordType.MF, ttl);