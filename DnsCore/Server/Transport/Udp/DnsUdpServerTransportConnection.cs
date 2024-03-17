using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport.Udp;

internal sealed class DnsUdpServerTransportConnection : DnsServerTransportConnection
{
    private readonly Socket _socket;
    private DnsServerTransportRequest? _request;

    public override DnsTransportType TransportType => DnsTransportType.UDP;
    public override ushort DefaultMessageSize => DnsDefaults.DefaultUdpMessageSize;
    public override ushort MaxMessageSize => DnsDefaults.MaxUdpMessageSize;
    public override EndPoint RemoteEndPoint { get; }

    internal DnsUdpServerTransportConnection(Socket socket, EndPoint remoteEndPoint, DnsServerTransportRequest request)
    {
        _socket = socket;
        RemoteEndPoint = remoteEndPoint;
        _request = request;
    }

    public override void Dispose() => _request?.Dispose();

    public override ValueTask<DnsServerTransportRequest?> Receive(CancellationToken cancellationToken)
    {
        DnsServerTransportRequest? request;
        if (_request is null)
            request = null;
        else
        {
            request = _request;
            _request = null;
        }
        return ValueTask.FromResult(request);
    }

    public override async ValueTask Send(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            await _socket.SendToAsync(buffer, SocketFlags.None, RemoteEndPoint, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException e)
        {
            throw new DnsServerTransportException("Failed to send response", e);
        }
    }
}