using System;

using DnsCore.Encoding;

namespace DnsCore.Model;

public sealed class DnsRawRecord(DnsName name, byte[] data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    : DnsRecord<byte[]>(name, data, recordType, @class, ttl)
{
    private protected override string DataToString() => BitConverter.ToString(Data);
    private protected override void EncodeData(ref DnsWriter writer) => writer.Write(Data);
    internal static byte[] DecodeData(ref DnsReader reader) => reader.ReadToEnd().ToArray();
}