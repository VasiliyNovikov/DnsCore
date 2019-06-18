using System;

namespace DnsCore.Model;

internal static class DnsMessageIdGenerator
{
    public static ushort NextId() => (ushort)Random.Shared.Next(UInt16.MaxValue);
}