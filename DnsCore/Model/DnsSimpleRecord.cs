using System;

namespace DnsCore.Model;

public abstract class DnsSimpleRecord<T>(DnsName name, T data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    : DnsRecord<T>(name, data, recordType, @class, ttl) where T : notnull;