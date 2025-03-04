using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordRawDataEncoder : DnsRecordDataEncoder<byte[]>
{
    public static readonly DnsRecordRawDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, byte[] data) =>  writer.Write(data);
    protected override byte[] DecodeData(ref DnsReader reader) => reader.ReadToEnd().ToArray();
    protected override DnsRecord<byte[]> CreateRecord(DnsName name, byte[] data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsRawRecord(name, data, recordType, @class, ttl);
}