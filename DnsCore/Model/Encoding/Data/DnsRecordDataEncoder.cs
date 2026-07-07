using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal abstract class DnsRecordDataEncoder
{
    public virtual void Encode(ref DnsWriter writer, DnsRecord record)
    {
        if (record is DnsRawRecord rawRecord)
            writer.Write(rawRecord.Data);
        else
            throw new NotSupportedException($"Encoding of {record.GetType().Name} is not supported.");
    }

    public abstract DnsRecord Decode(ref DnsReader reader, DnsName name, DnsRecordType recordType, DnsClass @class, TimeSpan ttl);
}

internal abstract class DnsRecordDataEncoder<T> : DnsRecordDataEncoder where T : notnull
{
    public override void Encode(ref DnsWriter writer, DnsRecord record)
    {
        if (record is DnsRecord<T> typedRecord)
            EncodeData(ref writer, typedRecord.Data);
        else
            base.Encode(ref writer, record);
    }

    public override DnsRecord Decode(ref DnsReader reader, DnsName name, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        var data = DecodeData(ref reader);
        return CreateRecord(name, data, recordType, @class, ttl);
    }

    protected abstract void EncodeData(ref DnsWriter writer, T data);
    protected abstract T DecodeData(ref DnsReader reader);
    protected abstract DnsRecord<T> CreateRecord(DnsName name, T data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl);
}