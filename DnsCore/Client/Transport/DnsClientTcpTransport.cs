using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Client.Transport;

internal sealed class DnsClientTcpTransport(EndPoint remoteEndPoint) : DnsClientSocketTransport(remoteEndPoint, SocketType.Stream, ProtocolType.Tcp)
{
    private readonly Channel<DnsTransportMessage> _receiveChannel = Channel.CreateUnbounded<DnsTransportMessage>();

    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public override async ValueTask Send(DnsTransportMessage requestMessage, CancellationToken cancellationToken)
    {
        using var socket = CreateSocket();
        socket.NoDelay = true;
        try
        {
            await socket.ConnectAsync(RemoteEndPoint, cancellationToken).ConfigureAwait(false);
            await socket.SendTcpMessage(requestMessage, cancellationToken).ConfigureAwait(false);
            var responseMessage = await socket.ReceiveTcpMessage(cancellationToken).ConfigureAwait(false)
                                  ?? throw new DnsClientTransportException("Failed to receive response");
            await _receiveChannel.Writer.WriteAsync(responseMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (DnsSocketException e)
        {
            throw new DnsClientTransportException("Failed to send request", e);
        }
    }

    public override async ValueTask<DnsTransportMessage> Receive(CancellationToken cancellationToken)
    {
        return await _receiveChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
    }
}