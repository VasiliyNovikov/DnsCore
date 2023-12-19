using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport;

public sealed class DnsUdpTransport : DnsTransport
{
    private const int MaxMessageSize = 512;

    private readonly IPEndPoint _remoteEndPoint;
    private readonly Socket _socket;

    public DnsUdpTransport(EndPoint endPoint)
    {
        _remoteEndPoint = new IPEndPoint(endPoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
        _socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(endPoint);
    }

    public override void Dispose() => _socket.Dispose();

    public override async ValueTask<DnsTransportConnection> Accept(CancellationToken cancellationToken)
    {
        var buffer = DnsTransportRequest.AllocateBuffer(MaxMessageSize);
        try
        {
            var result = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, _remoteEndPoint, cancellationToken).ConfigureAwait(false);
            return new DnsUdpTransportConnection(_socket, result.RemoteEndPoint, new DnsTransportRequest(buffer, result.ReceivedBytes));
        }
        catch (SocketException e)
        {
            throw new DnsTransportException("Failed to receive request", e);
        }
    }
}