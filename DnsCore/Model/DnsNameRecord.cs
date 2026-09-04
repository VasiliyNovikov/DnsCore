using System;

namespace DnsCore.Model;

public abstract class DnsNameRecord : DnsRecord<DnsName>
{
    protected DnsNameRecord(DnsName name, DnsName data, DnsRecordType recordType, TimeSpan ttl)
        : this(name, data, recordType, DnsClass.IN, ttl) { }

    private protected DnsNameRecord(DnsName name, DnsName data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
        : base(name, data, recordType, @class, ttl) { }
}
