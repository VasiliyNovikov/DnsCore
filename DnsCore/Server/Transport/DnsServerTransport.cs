using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport;

public abstract class DnsServerTransport : IDisposable
{
    public abstract ushort DefaultMessageSize { get; }
    public abstract ushort MaxMessageSize { get; }

    public abstract void Dispose();
    public abstract ValueTask<DnsServerTransportConnection> Accept(CancellationToken cancellationToken);
    
    public static DnsServerTransport Create(EndPoint endPoint, DnsTransportType type = DnsTransportType.UDP)
    {
        return type switch
        {
            DnsTransportType.UDP => new DnsUdpServerTransport(endPoint),
            DnsTransportType.TCP => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}