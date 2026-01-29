using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Client.Resolver;

internal abstract class SocketPool(ProtocolType protocol, EndPoint endPoint) : IAsyncDisposable
{
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public virtual async ValueTask<Socket> Acquire(CancellationToken cancellationToken)
    {
        var socket = new Socket(endPoint.AddressFamily, protocol == ProtocolType.Udp ? SocketType.Dgram : SocketType.Stream, protocol);
        try
        {
            socket.Bind(new IPEndPoint(endPoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0));
            await socket.ConnectDns(endPoint, cancellationToken).ConfigureAwait(false);
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public virtual ValueTask Release(Socket socket)
    {
        socket.Dispose();
        return ValueTask.CompletedTask;
    }
}