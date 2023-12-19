using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport;

public abstract class DnsTransportConnection : IDisposable
{
    public abstract EndPoint RemoteEndPoint { get; }
    public abstract void Dispose();
    public abstract ValueTask<DnsTransportRequest?> Receive(CancellationToken cancellationToken);
    public abstract ValueTask Send(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
}