using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport;

public sealed class DnsUdpTransportConnection : DnsTransportConnection
{
    private readonly Socket _socket;
    private DnsTransportRequest? _request;

    public override EndPoint RemoteEndPoint { get; }

    internal DnsUdpTransportConnection(Socket socket, EndPoint remoteEndPoint, DnsTransportRequest request)
    {
        _socket = socket;
        RemoteEndPoint = remoteEndPoint;
        _request = request;
    }

    public override void Dispose() => _request?.Dispose();

    public override ValueTask<DnsTransportRequest?> Receive(CancellationToken cancellationToken)
    {
        DnsTransportRequest? request;
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
            throw new DnsTransportException("Failed to send response", e);
        }
    }
}