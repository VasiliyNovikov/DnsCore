using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordRawDataEncoder : DnsRecordDataEncoder<byte[]>
{
    public static readonly DnsRecordRawDataEncoder Instance = new();

    public override void Validate(DnsRecord record)
    {
        if (record is not DnsRawRecord)
            throw new NotSupportedException($"Encoding of {record.GetType().Name} is not supported");
    }

    public override void Encode(ref DnsWriter writer, DnsRecord record)
    {
        Validate(record);
        base.Encode(ref writer, record);
    }

    protected override void EncodeData(ref DnsWriter writer, byte[] data) => writer.Write(data);
    protected override byte[] DecodeData(ref DnsReader reader) => reader.ReadToEnd().ToArray();
    protected override DnsRecord<byte[]> CreateRecord(DnsName name, byte[] data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsRawRecord(name, data, recordType, @class, ttl);
}