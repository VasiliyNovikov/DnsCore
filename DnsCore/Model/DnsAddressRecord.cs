using System;
using System.Net.Sockets;
using System.Net;

using DnsCore.Encoding;

namespace DnsCore.Model;

public sealed class DnsAddressRecord(DnsName name, IPAddress address, TimeSpan ttl)
    : DnsRecord(name, (address ?? throw new ArgumentNullException(nameof(address))).AddressFamily == AddressFamily.InterNetwork ? DnsRecordType.A : DnsRecordType.AAAA, DnsClass.IN, ttl)
{
    public IPAddress Address { get; } = address;

    public override string ToString() => $"{base.ToString()} {Address}";

    private protected override void EncodeData(ref DnsWriter writer)
    {
        var length = Address.AddressFamily == AddressFamily.InterNetwork ? 4 : 16;
        Address.TryWriteBytes(writer.Advance(length).Buffer, out _);
    }

    internal static IPAddress DecodeData(ref DnsReader reader) => new(reader.ReadToEnd());
}