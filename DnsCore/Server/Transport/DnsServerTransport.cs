using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Server.Transport;

internal abstract class DnsServerTransport : IAsyncDisposable
{
    public abstract ValueTask DisposeAsync();
    public abstract ValueTask<DnsServerTransportConnection> Accept(CancellationToken cancellationToken);

    public static DnsServerTransport Create(DnsTransportType type, EndPoint[] endPoints)
    {
        if (endPoints.Length == 0)
            throw new ArgumentException("At least one endpoint must be provided", nameof(endPoints));
        
        List<DnsServerTransport> transports = [];
        foreach (var endPoint in endPoints)
        {
            switch (type)
            {
                case DnsTransportType.UDP:
                    transports.Add(new Udp.DnsUdpServerTransport(endPoint));
                    break;
                case DnsTransportType.TCP:
                    transports.Add(new Tcp.DnsTcpServerTransport(endPoint));
                    break;
                case DnsTransportType.All:
                    transports.Add(new Udp.DnsUdpServerTransport(endPoint));
                    transports.Add(new Tcp.DnsTcpServerTransport(endPoint));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        return transports.Count == 1
            ? transports[0]
            : new Hybrid.DnsServerHybridTransport(transports.ToArray());
    }
}