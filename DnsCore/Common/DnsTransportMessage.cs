using System;

namespace DnsCore.Common;

internal sealed class DnsTransportMessage(DnsTransportBuffer buffer) : IDisposable
{
    public DnsTransportBuffer Buffer { get; } = buffer.Move();
    public void Dispose() => Buffer.Dispose();
}