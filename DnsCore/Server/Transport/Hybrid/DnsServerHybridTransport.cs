using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport.Hybrid;

internal sealed class DnsServerHybridTransport : DnsServerTransport
{
    private readonly DnsServerTransport[] _transports;
    private readonly Task<DnsServerTransportConnection>?[] _pendingAcceptTasks;

    internal DnsServerHybridTransport(IReadOnlyCollection<DnsServerTransport> transports)
    {
        _transports = [..transports];
        _pendingAcceptTasks = new Task<DnsServerTransportConnection>?[transports.Count];
    }

    public override async ValueTask DisposeAsync()
    {
        foreach (var transport in _transports)
            await transport.DisposeAsync().ConfigureAwait(false);

        foreach (var pendingAcceptTask in _pendingAcceptTasks)
        {
            if (pendingAcceptTask is null)
                continue;

            try
            {
                await pendingAcceptTask.ConfigureAwait(false);
            }
            catch (DnsServerTransportException)
            {
                // ignored
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }
    }

    public override async ValueTask<DnsServerTransportConnection> Accept(CancellationToken cancellationToken)
    {
        for (var i = 0; i < _pendingAcceptTasks.Length; ++i)
        {
            var pendingAcceptTask = _pendingAcceptTasks[i];
            if (pendingAcceptTask is { IsCompleted: true })
            {
                _pendingAcceptTasks[i] = null;
                return await pendingAcceptTask.ConfigureAwait(false);
            }
        }
        
        for (var i = 0; i < _transports.Length; ++i)
            _pendingAcceptTasks[i] ??= _transports[i].Accept(cancellationToken).AsTask();

        var completedTask = await Task.WhenAny(_pendingAcceptTasks!).ConfigureAwait(false);
        for (var i = 0; i < _pendingAcceptTasks.Length; ++i)
        {
            if (_pendingAcceptTasks[i] == completedTask)
            {
                _pendingAcceptTasks[i] = null;
                break;
            }
        }

        return await completedTask.ConfigureAwait(false);
    }
}