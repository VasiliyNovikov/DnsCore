namespace DnsCore.Model;

public enum DnsServiceParameterKey : ushort
{
    Mandatory = 0,
    Alpn = 1,
    NoDefaultAlpn = 2,
    Port = 3,
    IPv4Hint = 4,
    Ech = 5,
    IPv6Hint = 6,
    Reserved = 65535,
}
