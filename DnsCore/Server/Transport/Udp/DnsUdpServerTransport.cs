using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Server.Transport.Udp;

internal sealed class DnsUdpServerTransport(EndPoint endPoint) : DnsServerSocketTransport(endPoint, SocketType.Dgram, ProtocolType.Udp)
{
    private readonly IPEndPoint _remoteEndPointPlaceholder = new(endPoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);

    public override DnsTransportType Type => DnsTransportType.UDP;

    public override async ValueTask<DnsServerTransportConnection> Accept(CancellationToken cancellationToken)
    {
        try
        {
            var (remoteEndPoint, message) = await Socket.ReceiveUdpMessageFrom(_remoteEndPointPlaceholder, cancellationToken).ConfigureAwait(false);
            return new DnsUdpServerTransportConnection(Socket, remoteEndPoint, message);
        }
        catch (DnsSocketException e)
        {
            throw new DnsServerTransportException("Failed to receive request", e);
        }
    }
}