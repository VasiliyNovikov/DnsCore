using System.Security.Cryptography;

namespace DnsCore.Model;

internal static class DnsRequestIdGenerator
{
    public static ushort NextId() => (ushort)RandomNumberGenerator.GetInt32(ushort.MaxValue);
}