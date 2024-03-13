using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Internal;

namespace DnsCore.Server.Transport;

public sealed class DnsUdpServerTransport : DnsServerTransport
{
    public override ushort DefaultMessageSize => DnsDefaults.MaxUdpMessageSize / 2;
    public override ushort MaxMessageSize => DnsDefaults.MaxUdpMessageSize;

    private readonly IPEndPoint _remoteEndPointPlaceholder;
    private readonly Socket _socket;

    public DnsUdpServerTransport(EndPoint endPoint)
    {
        _remoteEndPointPlaceholder = new IPEndPoint(endPoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
        _socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(endPoint);
    }

    public override void Dispose() => _socket.Dispose();

    public override async ValueTask<DnsServerTransportConnection> Accept(CancellationToken cancellationToken)
    {
        var buffer = DnsBufferPool.Rent(DnsDefaults.MaxUdpMessageSize);
        try
        {
            var result = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, _remoteEndPointPlaceholder, cancellationToken).ConfigureAwait(false);
            return new DnsUdpServerTransportConnection(_socket, result.RemoteEndPoint, new DnsServerTransportRequest(buffer, result.ReceivedBytes));
        }
        catch (SocketException e)
        {
            throw new DnsServerTransportException("Failed to receive request", e);
        }
    }
}