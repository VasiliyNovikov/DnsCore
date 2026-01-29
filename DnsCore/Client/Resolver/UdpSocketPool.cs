using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using RentedListCore;

namespace DnsCore.Client.Resolver;

internal sealed class UdpSocketPool : SocketPool
{
    private readonly DnsClientUdpOptions _options;
    private readonly Stopwatch _timer = Stopwatch.StartNew();
    private readonly Queue<(Socket Socket, TimeSpan Timestamp)> _sockets = new();
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _cleanupCancellation = new();
    private readonly Task _cleanupTask;

    public UdpSocketPool(EndPoint endPoint, DnsClientUdpOptions options)
        : base(ProtocolType.Udp, endPoint)
    {
        _options = options;
        _cleanupTask = Task.Factory.StartNew(Cleanup, TaskCreationOptions.LongRunning);
    }

    public override async ValueTask DisposeAsync()
    {
        await _cleanupCancellation.CancelAsync().ConfigureAwait(false);
        await _cleanupTask.ConfigureAwait(false);
        foreach (var (socket, _) in _sockets)
            await base.Release(socket).ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    public override async ValueTask<Socket> Acquire(CancellationToken cancellationToken)
    {
        using RentedList<Socket> socketsToDispose = [];
        try
        {
            lock (_lock)
                while (_sockets.TryDequeue(out var item))
                    if (_timer.Elapsed < item.Timestamp + _options.SocketLifeTime)
                        return item.Socket;
                    else
                        socketsToDispose.Add(item.Socket);
            return await base.Acquire(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            foreach (var socket in socketsToDispose)
                await base.Release(socket).ConfigureAwait(false);
        }
    }

    public override async ValueTask Release(Socket socket)
    {
        bool shouldDispose;
        lock (_lock)
            if (!(shouldDispose = _sockets.Count >= _options.MaxSocketCount))
                _sockets.Enqueue((socket, _timer.Elapsed));
        if (shouldDispose)
            await base.Release(socket).ConfigureAwait(false);
    }

    private async Task Cleanup()
    {
        try
        {
            while (true)
            {
                await Task.Delay(_options.SocketIdleTime * 0.5, _cleanupCancellation.Token).ConfigureAwait(false);
                var now = _timer.Elapsed;
                using RentedList<Socket> socketsToDispose = [];
                lock (_lock)
                    while (_sockets.TryPeek(out var item))
                    {
                        var lifetime = now - item.Timestamp;
                        if (lifetime > _options.SocketLifeTime ||
                            lifetime > _options.SocketIdleTime && _sockets.Count > _options.MinSocketCount)
                        {
                            _sockets.Dequeue();
                            socketsToDispose.Add(item.Socket);
                            continue;
                        }
                        break;
                    }
                foreach (var socket in socketsToDispose)
                    await base.Release(socket).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_cleanupCancellation.IsCancellationRequested)
        {
            // Ignore
        }
    }
}