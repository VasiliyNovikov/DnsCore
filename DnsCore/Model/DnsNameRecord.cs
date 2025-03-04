using System;

namespace DnsCore.Model;

public abstract class DnsNameRecord(DnsName name, DnsName data, DnsRecordType recordType, TimeSpan ttl)
    : DnsRecord<DnsName>(name, data, recordType, DnsClass.IN, ttl);