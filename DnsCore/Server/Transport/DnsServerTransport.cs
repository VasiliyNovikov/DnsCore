using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Server.Transport;

internal abstract class DnsServerTransport(EndPoint endPoint) : IAsyncDisposable
{
    public EndPoint EndPoint => endPoint;
    public abstract DnsTransportType Type { get; }
    public abstract ValueTask DisposeAsync();
    public abstract ValueTask<DnsServerTransportConnection> Accept(CancellationToken cancellationToken);

    public static IEnumerable<DnsServerTransport> Create(DnsTransportType type, IReadOnlyCollection<EndPoint> endPoints)
    {
        if (endPoints.Count == 0)
            throw new ArgumentException("At least one endpoint must be provided", nameof(endPoints));

        foreach (var endPoint in endPoints)
        {
            switch (type)
            {
                case DnsTransportType.UDP:
                    yield return new Udp.DnsUdpServerTransport(endPoint);
                    break;
                case DnsTransportType.TCP:
                    yield return new Tcp.DnsTcpServerTransport(endPoint);
                    break;
                case DnsTransportType.All:
                    yield return new Udp.DnsUdpServerTransport(endPoint);
                    yield return new Tcp.DnsTcpServerTransport(endPoint);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}