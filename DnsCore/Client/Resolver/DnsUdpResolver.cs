using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Client.Resolver;

internal class DnsUdpResolver(EndPoint endPoint, DnsClientUdpOptions options) : DnsResolver
{
    protected override SocketPool Pool { get; } = new UdpSocketPool(endPoint, options);
    protected override async ValueTask<DnsTransportMessage> ReceiveMessage(Socket socket, CancellationToken cancellationToken) => await socket.ReceiveUdpMessage(cancellationToken).ConfigureAwait(false);
    protected override async ValueTask SendMessage(Socket socket, DnsTransportMessage message, CancellationToken cancellationToken) => await socket.SendUdpMessage(message, cancellationToken).ConfigureAwait(false);
}