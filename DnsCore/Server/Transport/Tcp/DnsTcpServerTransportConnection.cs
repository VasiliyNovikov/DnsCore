using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Server.Transport.Tcp;

internal sealed class DnsTcpServerTransportConnection : DnsServerTransportConnection
{
    private readonly Socket _socket;

    public override DnsTransportType TransportType => DnsTransportType.TCP;
    public override ushort DefaultMessageSize => DnsDefaults.DefaultTcpMessageSize;
    public override ushort MaxMessageSize => DnsDefaults.MaxTcpMessageSize;
    public override EndPoint RemoteEndPoint => _socket.RemoteEndPoint!;

    internal DnsTcpServerTransportConnection(Socket socket) => _socket = socket;

    public override void Dispose() => _socket.Dispose();

    public override async ValueTask<DnsTransportMessage?> Receive(CancellationToken cancellationToken)
    {
        try
        {
            return await _socket.ReceiveTcpMessage(cancellationToken).ConfigureAwait(false);
        }
        catch (DnsSocketException e)
        {
            throw new DnsServerTransportException("Failed to receive request", e);
        }
    }

    public override async ValueTask Send(DnsTransportMessage responseMessage, CancellationToken cancellationToken)
    {
        try
        {
            await _socket.SendTcpMessage(responseMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (DnsSocketException e)
        {
            throw new DnsServerTransportException("Failed to send response", e);
        }
    }
}