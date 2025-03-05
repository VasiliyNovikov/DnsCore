using System;

namespace DnsCore.Common;

internal sealed class DnsTransportMessage(DnsTransportBuffer buffer, bool ownsBuffer = true) : IDisposable
{
    public DnsTransportBuffer Buffer { get; } = ownsBuffer ? buffer.Move() : buffer;

    public void Dispose()
    {
        if (ownsBuffer)
            Buffer.Dispose();
    }
}