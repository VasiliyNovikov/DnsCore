using System;

namespace DnsCore.Common;

internal sealed class DnsTransportMessage(byte[] buffer, int length, bool ownsBuffer = true) : IDisposable
{
    public ReadOnlyMemory<byte> Buffer => buffer.AsMemory(0, length);

    public void Dispose()
    {
        if (ownsBuffer)
            DnsBufferPool.Return(buffer);
    }
}