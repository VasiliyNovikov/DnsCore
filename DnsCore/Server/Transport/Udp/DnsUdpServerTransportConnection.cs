using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Server.Transport.Udp;

internal sealed class DnsUdpServerTransportConnection : DnsServerTransportConnection
{
    private readonly Socket _socket;
    private DnsTransportMessage? _message;

    public override DnsTransportType TransportType => DnsTransportType.UDP;
    public override ushort DefaultMessageSize => DnsDefaults.DefaultUdpMessageSize;
    public override ushort MaxMessageSize => DnsDefaults.MaxUdpMessageSize;
    public override EndPoint RemoteEndPoint { get; }

    internal DnsUdpServerTransportConnection(Socket socket, EndPoint remoteEndPoint, DnsTransportMessage message)
    {
        _socket = socket;
        RemoteEndPoint = remoteEndPoint;
        _message = message;
    }

    public override void Dispose() => _message?.Dispose();

    public override ValueTask<DnsTransportMessage?> Receive(CancellationToken cancellationToken)
    {
        DnsTransportMessage? message;
        if (_message is null)
            message = null;
        else
        {
            message = _message;
            _message = null;
        }
        return ValueTask.FromResult(message);
    }

    public override async ValueTask Send(DnsTransportMessage responseMessage, CancellationToken cancellationToken)
    {
        try
        {
            await _socket.SendUdpMessageTo(responseMessage, RemoteEndPoint, cancellationToken).ConfigureAwait(false);
        }
        catch (DnsSocketException e)
        {
            throw new DnsServerTransportException("Failed to send response", e);
        }
    }
}