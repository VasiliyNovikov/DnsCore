using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DnsCore.Utils;

namespace DnsCore.Server.Transport.Hybrid;

internal sealed class DnsServerHybridTransport : DnsServerTransport
{
    private const int ConnectionQueueSize = 256;

    private readonly CancellationTokenSource _cancellation = new();
    private readonly Channel<Task<DnsServerTransportConnection>> _connectionTaskQueue = Channel.CreateBounded<Task<DnsServerTransportConnection>>(ConnectionQueueSize);
    private readonly Task[] _transportWorkers;

    internal DnsServerHybridTransport(IReadOnlyList<DnsServerTransport> transports)
    {
        _transportWorkers = new Task[transports.Count];
        for (var i = 0; i < transports.Count; ++i)
            _transportWorkers[i] = TransportWorker(transports[i]);
    }

    public override async ValueTask DisposeAsync()
    {
        await _cancellation.CancelAsync().ConfigureAwait(false);
        foreach (var transportWorker in _transportWorkers)
            await transportWorker.ConfigureAwait(false);
        _cancellation.Dispose();
        _connectionTaskQueue.Writer.Complete();
        await foreach (var connectionTask in _connectionTaskQueue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                await connectionTask.ConfigureAwait(false);
            }
            catch (DnsServerTransportException)
            {
                // ignored
            }
        }
    }

    public override async ValueTask<DnsServerTransportConnection> Accept(CancellationToken cancellationToken)
    {
        var connectionTask = await _connectionTaskQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return await connectionTask.ConfigureAwait(false);
    }

    private async Task TransportWorker(DnsServerTransport transport)
    {
        var cancellationToken = _cancellation.Token;
        await using (transport.ConfigureAwait(false))
        {
            while (true)
            {
                var connectionTask = await transport.Accept(cancellationToken).WhenCompleted().ConfigureAwait(false);
                try
                {
                    await _connectionTaskQueue.Writer.WriteAsync(connectionTask, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        await connectionTask.ConfigureAwait(false);
                    }
                    catch (DnsServerTransportException)
                    {
                        // ignored
                    }
                    catch (OperationCanceledException)
                    {
                        // ignored
                    }
                    break;
                }
            }
        }
    }
}