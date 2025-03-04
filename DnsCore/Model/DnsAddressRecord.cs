using System;
using System.Net.Sockets;
using System.Net;

namespace DnsCore.Model;

public sealed class DnsAddressRecord(DnsName name, IPAddress data, TimeSpan ttl)
    : DnsRecord<IPAddress>(name, data, data.AddressFamily == AddressFamily.InterNetwork ? DnsRecordType.A : DnsRecordType.AAAA, DnsClass.IN, ttl);