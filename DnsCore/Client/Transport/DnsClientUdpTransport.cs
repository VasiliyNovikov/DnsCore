using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Client.Transport;

internal sealed class DnsClientUdpTransport : DnsClientSocketTransport
{
    private readonly Socket _socket;

    public DnsClientUdpTransport(EndPoint remoteEndPoint) : base(remoteEndPoint, SocketType.Dgram, ProtocolType.Udp)
    {
        _socket = CreateSocket();
        _socket.Connect(remoteEndPoint);
    }

    public override ValueTask DisposeAsync()
    {
        try
        {
            _socket.Dispose();
            return ValueTask.CompletedTask;
        }
        catch (Exception e)
        {
            return ValueTask.FromException(e);
        }
    }

    public override async ValueTask Send(DnsTransportMessage requestMessage, CancellationToken cancellationToken)
    {
        try
        {
            await _socket.SendUdpMessage(requestMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (DnsSocketException e)
        {
            throw new DnsClientTransportException("Failed to send request", e);
        }
    }

    public override async ValueTask<DnsTransportMessage> Receive(CancellationToken cancellationToken)
    {
        try
        {
            return await _socket.ReceiveUdpMessage(cancellationToken).ConfigureAwait(false);
        }
        catch (DnsSocketException e)
        {
            throw new DnsClientTransportException("Failed to receive response", e);
        }
    }
}