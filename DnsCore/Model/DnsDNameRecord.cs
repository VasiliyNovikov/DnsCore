using System;

namespace DnsCore.Model;

public sealed class DnsDNameRecord : DnsNameRecord
{
    public DnsDNameRecord(DnsName name, DnsName data, TimeSpan ttl)
        : this(name, data, DnsClass.IN, ttl) { }

    internal DnsDNameRecord(DnsName name, DnsName data, DnsClass @class, TimeSpan ttl)
        : base(name, data, DnsRecordType.DNAME, @class, ttl) { }
}
