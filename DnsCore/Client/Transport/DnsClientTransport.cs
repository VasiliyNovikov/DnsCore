using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Client.Transport;

internal abstract class DnsClientTransport : IAsyncDisposable
{
    public abstract ValueTask DisposeAsync();
    public abstract ValueTask Send(DnsTransportMessage requestMessage, CancellationToken cancellationToken);
    public abstract ValueTask<DnsTransportMessage> Receive(CancellationToken cancellationToken);

    public static DnsClientTransport Create(DnsTransportType type, EndPoint endPoint)
    {
        switch (type)
        {
            case DnsTransportType.UDP:
                return new DnsClientUdpTransport(endPoint);
            case DnsTransportType.TCP:
                return new DnsClientTcpTransport(endPoint);
            case DnsTransportType.All:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}