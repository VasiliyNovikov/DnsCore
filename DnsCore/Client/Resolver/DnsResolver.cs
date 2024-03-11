using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;
using DnsCore.Model;
using DnsCore.Model.Encoding;

namespace DnsCore.Client.Resolver;

internal abstract class DnsResolver : IAsyncDisposable
{
    protected abstract SocketPool Pool { get; }

    public async ValueTask<DnsResponse> Resolve(DnsRequest request, CancellationToken cancellationToken)
    {
        var socket = await Pool.Acquire(cancellationToken).ConfigureAwait(false);
        try
        {
            using var buffer = new DnsTransportBuffer(DnsDefaults.MaxUdpMessageSize);
            buffer.Resize(DnsRequestEncoder.Encode(buffer.Span, request));
            using var requestMessage = new DnsTransportMessage(buffer);
            await SendMessage(socket, requestMessage, cancellationToken).ConfigureAwait(false);
            while (true)
            {
                using var responseMessage = await ReceiveMessage(socket, cancellationToken).ConfigureAwait(false);
                var response = DnsResponseEncoder.Decode(responseMessage.Buffer.Span);
                if (response.Id != request.Id) // Some old pending messages
                    continue;
                return response;
            }
        }
        finally
        {
            await Pool.Release(socket).ConfigureAwait(false);
        }
    }

    public virtual async ValueTask DisposeAsync() => await Pool.DisposeAsync().ConfigureAwait(false);

    protected abstract ValueTask SendMessage(Socket socket, DnsTransportMessage message, CancellationToken cancellationToken);
    protected abstract ValueTask<DnsTransportMessage> ReceiveMessage(Socket socket, CancellationToken cancellationToken);
}