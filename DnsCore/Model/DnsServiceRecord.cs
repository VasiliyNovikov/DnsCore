using System;

namespace DnsCore.Model;

public sealed class DnsServiceRecord(DnsName name, ushort priority, ushort weight, ushort port, DnsName target, TimeSpan ttl)
    : DnsRecord<DnsServiceRecordData>(name, new(priority, weight, port, target), DnsRecordType.SRV, DnsClass.IN, ttl);