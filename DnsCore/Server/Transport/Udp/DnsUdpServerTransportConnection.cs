using System;
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
        DnsTransportMessage? request;
        if (_message is null)
            request = null;
        else
        {
            request = _message;
            _message = null;
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