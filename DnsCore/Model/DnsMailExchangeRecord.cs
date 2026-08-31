using System;

namespace DnsCore.Model;

public sealed class DnsMailExchangeRecord(DnsName name, ushort preference, DnsName exchange, TimeSpan ttl)
    : DnsRecord<DnsMailExchangeRecordData>(name, new(preference, exchange), DnsRecordType.MX, DnsClass.IN, ttl);