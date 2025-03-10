using System.Threading;

namespace DnsCore.Model;

internal static class DnsRequestIdGenerator
{
    private static uint _nextId;
    public static ushort NextId() => (ushort)Interlocked.Increment(ref _nextId);
}