using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Server.Transport.Udp;

internal sealed class DnsUdpServerTransport(EndPoint endPoint) : DnsServerSocketTransport(endPoint, SocketType.Dgram, ProtocolType.Udp)
{
    private readonly IPEndPoint _remoteEndPointPlaceholder = new(endPoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);

    public override async ValueTask<DnsServerTransportConnection> Accept(CancellationToken cancellationToken)
    {
        var buffer = DnsBufferPool.Rent(DnsDefaults.MaxUdpMessageSize);
        try
        {
            var result = await Socket.ReceiveFromAsync(buffer, SocketFlags.None, _remoteEndPointPlaceholder, cancellationToken).ConfigureAwait(false);
            return new DnsUdpServerTransportConnection(Socket, result.RemoteEndPoint, new DnsTransportMessage(buffer, result.ReceivedBytes));
        }
        catch (SocketException e)
        {
            throw new DnsServerTransportException("Failed to receive request", e);
        }
    }
}