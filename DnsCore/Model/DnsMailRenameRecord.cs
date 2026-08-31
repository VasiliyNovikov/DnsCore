using System;

namespace DnsCore.Model;

public sealed class DnsMailRenameRecord(DnsName name, DnsName data, TimeSpan ttl) : DnsNameRecord(name, data, DnsRecordType.MR, ttl);