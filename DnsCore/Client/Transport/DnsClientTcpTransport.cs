using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Client.Transport;

internal sealed class DnsClientTcpTransport(EndPoint remoteEndPoint) : DnsClientSocketTransport(remoteEndPoint, SocketType.Stream, ProtocolType.Tcp)
{
    private readonly Channel<Socket> _receiveChannel = Channel.CreateUnbounded<Socket>();

    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public override async ValueTask Send(DnsTransportMessage requestMessage, CancellationToken cancellationToken)
    {
        var socket = CreateSocket();
        try
        {
            socket.NoDelay = true;
            try
            {
                await socket.ConnectAsync(RemoteEndPoint, cancellationToken).ConfigureAwait(false);
                await socket.SendTcpMessage(requestMessage, cancellationToken).ConfigureAwait(false);
                await _receiveChannel.Writer.WriteAsync(socket, cancellationToken).ConfigureAwait(false);
            }
            catch (DnsSocketException e)
            {
                throw new DnsClientTransportException("Failed to send request", e);
            }
        }
        catch (Exception)
        {
            socket.Dispose();
            throw;
        }
    }

    public override async ValueTask<DnsTransportMessage> Receive(CancellationToken cancellationToken)
    {
        using var socket = await _receiveChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return await socket.ReceiveTcpMessage(cancellationToken).ConfigureAwait(false)
               ?? throw new DnsClientTransportException("Failed to receive response");
    }
}