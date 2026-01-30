using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DnsCore.Utils;

internal class ServerTaskScheduler : IDisposable
{
    private readonly Channel<Task> _tasks = Channel.CreateUnbounded<Task>();
    private readonly CancellationToken _externalToken;
    private readonly CancellationTokenSource _stopCts;

    private ServerTaskScheduler(CancellationToken externalToken)
    {
        _externalToken = externalToken;
        _stopCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
    }

    public void Dispose() => _stopCts.Dispose();

    public async ValueTask Enqueue(Func<ServerTaskScheduler, CancellationToken, ValueTask> task) => await _tasks.Writer.WriteAsync(EnqueueWrapper(task)).ConfigureAwait(false);

    public static async Task Run(Func<ServerTaskScheduler, CancellationToken, ValueTask> startTask, CancellationToken cancellationToken)
    {
        using var scheduler = new ServerTaskScheduler(cancellationToken);
        await scheduler.Enqueue(startTask).ConfigureAwait(false);
        await scheduler.Run().ConfigureAwait(false);
    }

    private async Task EnqueueWrapper(Func<ServerTaskScheduler, CancellationToken, ValueTask> taskFunc) => await taskFunc(this, _stopCts.Token).ConfigureAwait(false);

    private async Task Run()
    {
        var exceptions = new List<Exception>();
        var runningTasks = new HashSet<Task>();
        while (true)
        {
            while (_tasks.Reader.TryRead(out var task))
                runningTasks.Add(task);

            if (!_stopCts.IsCancellationRequested)
                runningTasks.Add(_tasks.Reader.WaitToReadAsync(_externalToken).AsTask());

            if (runningTasks.Count == 0)
                break;

            var completedTask = await Task.WhenAny(runningTasks).ConfigureAwait(false);
            runningTasks.Remove(completedTask);

            if (!completedTask.IsFaulted && !_externalToken.IsCancellationRequested)
                continue;

            if (completedTask.IsFaulted)
                exceptions.AddRange(completedTask.Exception.Flatten().InnerExceptions);

            if (_stopCts.IsCancellationRequested)
                continue;

            _tasks.Writer.Complete();
            await _stopCts.CancelAsync().ConfigureAwait(false);
        }

        switch (exceptions.Count)
        {
            case 0:
                _externalToken.ThrowIfCancellationRequested();
                break;
            case 1:
                ExceptionDispatchInfo.Throw(exceptions[0]);
                break;
            default:
                throw new AggregateException(exceptions);
        }
    }
}