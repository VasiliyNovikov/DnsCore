using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Server.Transport.Udp;

internal sealed class DnsUdpServerTransportConnection : DnsServerTransportConnection
{
    private readonly Socket _socket;
    private DnsTransportMessage? _request;

    public override DnsTransportType TransportType => DnsTransportType.UDP;
    public override ushort DefaultMessageSize => DnsDefaults.DefaultUdpMessageSize;
    public override ushort MaxMessageSize => DnsDefaults.MaxUdpMessageSize;
    public override EndPoint RemoteEndPoint { get; }

    internal DnsUdpServerTransportConnection(Socket socket, EndPoint remoteEndPoint, DnsTransportMessage request)
    {
        _socket = socket;
        RemoteEndPoint = remoteEndPoint;
        _request = request;
    }

    public override void Dispose() => _request?.Dispose();

    public override ValueTask<DnsTransportMessage?> Receive(CancellationToken cancellationToken)
    {
        DnsTransportMessage? request;
        if (_request is null)
            request = null;
        else
        {
            request = _request;
            _request = null;
        }
        return ValueTask.FromResult(request);
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