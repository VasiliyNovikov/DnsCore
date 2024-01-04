using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport;

public abstract class DnsTransport : IDisposable
{
    public abstract int MaxMessageSize { get; }

    public abstract void Dispose();
    public abstract ValueTask<DnsTransportConnection> Accept(CancellationToken cancellationToken);
    
    public static DnsTransport Create(EndPoint endPoint, DnsTransportType type = DnsTransportType.UDP)
    {
        return type switch
        {
            DnsTransportType.UDP => new DnsUdpTransport(endPoint),
            DnsTransportType.TCP => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}