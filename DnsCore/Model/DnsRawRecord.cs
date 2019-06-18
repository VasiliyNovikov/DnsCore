using System;

using DnsCore.Encoding;

namespace DnsCore.Model;

public sealed class DnsRawRecord(DnsName name, DnsRecordType recordType, DnsClass @class, TimeSpan ttl, byte[] data)
    : DnsRecord(name, recordType, @class, ttl)
{
    public byte[] Data { get; } = data;
    public override string ToString() => $"{base.ToString()} {BitConverter.ToString(Data)}";
    private protected override void EncodeData(ref DnsWriter writer) => writer.Write(Data);
    internal static byte[] DecodeData(ref DnsReader reader) => reader.ReadToEnd().ToArray();
}