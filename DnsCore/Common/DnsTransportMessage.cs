using System;

namespace DnsCore.Common;

internal sealed class DnsTransportMessage(byte[] buffer, int length) : IDisposable
{
    public ReadOnlySpan<byte> Buffer => buffer.AsSpan(0, length);

    public void Dispose() => DnsBufferPool.Return(buffer);
}