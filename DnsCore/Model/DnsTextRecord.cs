using System;
using System.Text;

namespace DnsCore.Model;

public class DnsTextRecord : DnsRecord<string>
{
    private const ushort MaxDataLength = 65535;
    internal static readonly System.Text.Encoding Encoding = new UTF8Encoding(false);

    public DnsTextRecord(DnsName name, string data, TimeSpan ttl) : base(name, data, DnsRecordType.TXT, DnsClass.IN, ttl)
    {
        if (Encoding.GetByteCount(data) > MaxDataLength)
            throw new ArgumentOutOfRangeException(nameof(data), $"The encoded data length exceeds the maximum allowed length of {MaxDataLength}.");
    }
}