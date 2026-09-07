using System;

namespace DnsCore.Model;

public sealed class DnsNamingAuthorityPointerRecord : DnsRecord<DnsNamingAuthorityPointerRecordData>
{
    public DnsNamingAuthorityPointerRecord(DnsName name, ushort order, ushort preference, string flags, string services, string regularExpression, DnsName replacement, TimeSpan ttl)
        : this(name, new(order, preference, flags, services, regularExpression, replacement), DnsClass.IN, ttl) { }

    internal DnsNamingAuthorityPointerRecord(DnsName name, DnsNamingAuthorityPointerRecordData data, DnsClass @class, TimeSpan ttl)
        : base(name, data, DnsRecordType.NAPTR, @class, ttl) { }
}