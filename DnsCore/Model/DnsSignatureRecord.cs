using System;

namespace DnsCore.Model;

public sealed class DnsSignatureRecord : DnsRecord<DnsSignatureRecordData>
{
    public DnsSignatureRecord(DnsName name, DnsRecordType typeCovered, byte algorithm, byte labels, TimeSpan originalTtl, uint signatureExpiration, uint signatureInception, ushort keyTag, DnsName signerName, DnsName? privateAlgorithmName, byte[] signature, TimeSpan ttl)
        : this(name, new(typeCovered, algorithm, labels, originalTtl, signatureExpiration, signatureInception, keyTag, signerName, privateAlgorithmName, signature), DnsClass.IN, ttl) { }

    internal DnsSignatureRecord(DnsName name, DnsSignatureRecordData data, DnsClass @class, TimeSpan ttl)
        : base(name, data, DnsRecordType.SIG, @class, ttl) { }
}