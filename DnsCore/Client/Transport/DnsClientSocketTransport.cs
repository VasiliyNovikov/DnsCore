using System.Net;
using System.Net.Sockets;

namespace DnsCore.Client.Transport;

internal abstract class DnsClientSocketTransport(EndPoint remoteEndPoint, SocketType socketType, ProtocolType protocolType) : DnsClientTransport
{
    protected EndPoint RemoteEndPoint { get; } = remoteEndPoint;

    protected Socket CreateSocket()
    {
        var socket = new Socket(RemoteEndPoint.AddressFamily, socketType, protocolType);
        socket.Bind(new IPEndPoint(RemoteEndPoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0));
        return socket;
    }
}