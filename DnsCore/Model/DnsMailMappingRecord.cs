using System;

namespace DnsCore.Model;

public sealed class DnsMailMappingRecord(DnsName name, ushort preference, DnsName map822, DnsName mapX400, TimeSpan ttl)
    : DnsRecord<DnsMailMappingRecordData>(name, new(preference, map822, mapX400), DnsRecordType.PX, DnsClass.IN, ttl);