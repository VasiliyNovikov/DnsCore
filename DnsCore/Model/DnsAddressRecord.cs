using System;
using System.Net.Sockets;
using System.Net;

using DnsCore.Encoding;

namespace DnsCore.Model;

public sealed class DnsAddressRecord(DnsName name, IPAddress data, TimeSpan ttl)
    : DnsSimpleRecord<IPAddress>(name, data, data.AddressFamily == AddressFamily.InterNetwork ? DnsRecordType.A : DnsRecordType.AAAA, DnsClass.IN, ttl)
{
    private protected override void EncodeData(ref DnsWriter writer)
    {
        var length = Data.AddressFamily == AddressFamily.InterNetwork ? 4 : 16;
        Data.TryWriteBytes(writer.ProvideBufferAndAdvance(length), out _);
    }

    internal static IPAddress DecodeData(ref DnsReader reader) => new(reader.ReadToEnd());
}