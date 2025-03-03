using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Server.Transport;

internal abstract class DnsServerTransportConnection : IDisposable
{
    public abstract DnsTransportType TransportType { get; }
    public abstract ushort DefaultMessageSize { get; }
    public abstract ushort MaxMessageSize { get; }
    public abstract EndPoint RemoteEndPoint { get; }
    public abstract void Dispose();
    public abstract ValueTask<DnsTransportMessage?> Receive(CancellationToken cancellationToken);
    public abstract ValueTask Send(DnsTransportMessage responseMessage, CancellationToken cancellationToken);
}