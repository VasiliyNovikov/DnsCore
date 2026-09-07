using System;

namespace DnsCore.Model;

public sealed class DnsAfsDatabaseRecord : DnsRecord<DnsAfsDatabaseRecordData>
{
    public DnsAfsDatabaseRecord(DnsName name, ushort subtype, DnsName hostname, TimeSpan ttl)
        : this(name, subtype, hostname, DnsClass.IN, ttl) { }

    internal DnsAfsDatabaseRecord(DnsName name, ushort subtype, DnsName hostname, DnsClass @class, TimeSpan ttl)
        : base(name, new(subtype, hostname), DnsRecordType.AFSDB, @class, ttl) { }
}