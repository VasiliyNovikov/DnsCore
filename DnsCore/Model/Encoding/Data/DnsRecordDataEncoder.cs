using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal abstract class DnsRecordDataEncoder
{
    public virtual void Validate(DnsRecord record)
    {
        if (record is not DnsRawRecord)
            throw new NotSupportedException($"Encoding of {record.GetType().Name} is not supported.");
    }

    public virtual void Encode(ref DnsWriter writer, DnsRecord record)
    {
        Validate(record);
        if (record is DnsRawRecord rawRecord)
            writer.Write(rawRecord.Data);
    }

    public abstract DnsRecord Decode(ref DnsReader reader, DnsName name, DnsRecordType recordType, DnsClass @class, TimeSpan ttl);
}

internal abstract class DnsRecordDataEncoder<T> : DnsRecordDataEncoder where T : notnull
{
    public override void Validate(DnsRecord record)
    {
        if (record is DnsRecord<T> typedRecord)
            ValidateData(typedRecord.Data);
        else
            base.Validate(record);
    }

    public override void Encode(ref DnsWriter writer, DnsRecord record)
    {
        if (record is DnsRecord<T> typedRecord)
        {
            ValidateData(typedRecord.Data);
            EncodeData(ref writer, typedRecord.Data);
        }
        else
            base.Encode(ref writer, record);
    }

    public override DnsRecord Decode(ref DnsReader reader, DnsName name, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        var data = DecodeData(ref reader);
        if (!reader.ReadToEnd().IsEmpty)
            throw new FormatException($"Invalid {recordType} record data: buffer contains extra data");
        ValidateData(data);

        return CreateRecord(name, data, recordType, @class, ttl);
    }

    protected abstract void EncodeData(ref DnsWriter writer, T data);
    protected abstract T DecodeData(ref DnsReader reader);
    protected abstract DnsRecord<T> CreateRecord(DnsName name, T data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl);
    protected virtual void ValidateData(T data) { }

    protected static int GetUncompressedLength(DnsName name) => name.IsEmpty ? 1 : checked(name.Length + 1);

    protected static void ValidateDataLength(int length)
    {
        if ((uint)length > UInt16.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(length), $"DNS record data exceeds the maximum length of {UInt16.MaxValue} bytes");
    }
}