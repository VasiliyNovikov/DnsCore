using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport.Tcp;

internal sealed class DnsTcpServerTransport : DnsServerSocketTransport
{
    public DnsTcpServerTransport(EndPoint endPoint)
        : base(endPoint, SocketType.Stream, ProtocolType.Tcp)
    {
        Socket.NoDelay = true;
        Socket.Listen();
    }

    public override async ValueTask<DnsServerTransportConnection> Accept(CancellationToken cancellationToken)
    {
        try
        {
            return new DnsTcpServerTransportConnection(await Socket.AcceptAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (SocketException e)
        {
            throw new DnsServerTransportException("Failed to accept a request connection", e);
        }
    }
}