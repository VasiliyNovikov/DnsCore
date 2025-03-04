using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal abstract class DnsRecordDataEncoder
{
    public abstract void Encode(ref DnsWriter writer, DnsRecord record);
    public abstract DnsRecord Decode(ref DnsReader reader, DnsName name,DnsRecordType recordType, DnsClass @class, TimeSpan ttl);
}

internal abstract class DnsRecordDataEncoder<T> : DnsRecordDataEncoder where T : notnull
{
    public override void Encode(ref DnsWriter writer, DnsRecord record) => EncodeData(ref writer, ((DnsRecord<T>)record).Data);

    public override DnsRecord Decode(ref DnsReader reader, DnsName name, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        var data = DecodeData(ref reader);
        return CreateRecord(name, data, recordType, @class, ttl);
    }

    protected abstract void EncodeData(ref DnsWriter writer, T data);
    protected abstract T DecodeData(ref DnsReader reader);
    protected abstract DnsRecord<T> CreateRecord(DnsName name, T data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl);
}