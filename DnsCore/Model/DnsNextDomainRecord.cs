using System;

namespace DnsCore.Model;

public sealed class DnsNextDomainRecord : DnsRecord<DnsNextDomainRecordData>
{
    public DnsNextDomainRecord(DnsName name, DnsName nextDomainName, byte[] typeBitmap, TimeSpan ttl)
        : this(name, new(nextDomainName, typeBitmap), DnsClass.IN, ttl) { }

    internal DnsNextDomainRecord(DnsName name, DnsNextDomainRecordData data, DnsClass @class, TimeSpan ttl)
        : base(name, data, DnsRecordType.NXT, @class, ttl) { }
}