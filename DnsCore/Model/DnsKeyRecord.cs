using System;

namespace DnsCore.Model;

public sealed class DnsKeyRecord : DnsRecord<DnsKeyRecordData>
{
    public DnsKeyRecord(DnsName name, ushort flags, byte protocol, byte algorithm, DnsName? privateAlgorithmName, byte[] publicKey, TimeSpan ttl)
        : this(name, new(flags, protocol, algorithm, privateAlgorithmName, publicKey), DnsClass.IN, ttl) { }

    internal DnsKeyRecord(DnsName name, DnsKeyRecordData data, DnsClass @class, TimeSpan ttl)
        : base(name, data, DnsRecordType.KEY, @class, ttl) { }
}