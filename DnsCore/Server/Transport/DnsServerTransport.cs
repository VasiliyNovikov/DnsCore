using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport;

internal abstract class DnsServerTransport : IAsyncDisposable
{
    public abstract ValueTask DisposeAsync();
    public abstract ValueTask<DnsServerTransportConnection> Accept(CancellationToken cancellationToken);

    public static DnsServerTransport Create(DnsTransportType type, EndPoint endPoint)
    {
        switch (type)
        {
            case DnsTransportType.UDP:
                return new Udp.DnsUdpServerTransport(endPoint);
            case DnsTransportType.TCP:
            case DnsTransportType.All:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}