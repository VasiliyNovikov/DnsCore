using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Client.Resolver;

internal class DnsTcpResolver(EndPoint endPoint) : DnsResolver
{
    protected override SocketPool Pool { get; } = new TcpSocketPool(endPoint);
    protected override async ValueTask<DnsTransportMessage> ReceiveMessage(Socket socket, CancellationToken cancellationToken) => await socket.ReceiveTcpMessage(cancellationToken).ConfigureAwait(false);
    protected override async ValueTask SendMessage(Socket socket, DnsTransportMessage message, CancellationToken cancellationToken) => await socket.SendTcpMessage(message, cancellationToken).ConfigureAwait(false);
}