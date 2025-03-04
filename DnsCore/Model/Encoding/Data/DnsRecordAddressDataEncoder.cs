using System;
using System.Net;
using System.Net.Sockets;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordAddressDataEncoder : DnsRecordDataEncoder<IPAddress>
{
    public static readonly DnsRecordAddressDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, IPAddress data) => data.TryWriteBytes(writer.ProvideBufferAndAdvance(data.AddressFamily == AddressFamily.InterNetwork ? 4 : 16), out _);
    protected override IPAddress DecodeData(ref DnsReader reader) => new(reader.ReadToEnd());
    protected override DnsRecord<IPAddress> CreateRecord(DnsName name, IPAddress data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsAddressRecord(name, data, ttl);
}