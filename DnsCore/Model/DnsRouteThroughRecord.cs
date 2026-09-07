using System;

namespace DnsCore.Model;

public sealed class DnsRouteThroughRecord : DnsRecord<DnsRouteThroughRecordData>
{
    public DnsRouteThroughRecord(DnsName name, ushort preference, DnsName intermediateHost, TimeSpan ttl)
        : this(name, preference, intermediateHost, DnsClass.IN, ttl) { }

    internal DnsRouteThroughRecord(DnsName name, ushort preference, DnsName intermediateHost, DnsClass @class, TimeSpan ttl)
        : base(name, new(preference, intermediateHost), DnsRecordType.RT, @class, ttl) { }
}