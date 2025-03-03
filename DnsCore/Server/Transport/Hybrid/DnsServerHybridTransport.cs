using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport.Hybrid;

internal sealed class DnsServerHybridTransport : DnsServerTransport
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Channel<Task<DnsServerTransportConnection>> _connectionTaskQueue = Channel.CreateUnbounded<Task<DnsServerTransportConnection>>();
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
        try
        {
            await using (transport.ConfigureAwait(false))
            {
                while (true)
                {
                    var connectionTask = transport.Accept(CancellationToken.None).AsTask();
                    await connectionTask.WaitAsync(_cancellation.Token).ConfigureAwait(false);
                    await _connectionTaskQueue.Writer.WriteAsync(connectionTask).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }
}