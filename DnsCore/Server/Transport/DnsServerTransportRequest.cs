using System;

using DnsCore.Internal;

namespace DnsCore.Server.Transport;

public sealed class DnsServerTransportRequest(byte[] buffer, int length) : IDisposable
{
    public ReadOnlySpan<byte> Buffer => buffer.AsSpan(0, length);

    public void Dispose() => DnsBufferPool.Return(buffer);
}