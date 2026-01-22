using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Utils;

internal class ServerTaskScheduler : IDisposable
{
    private readonly ConcurrentBag<Task> _tasks = [];
    private readonly CancellationToken _externalToken;
    private readonly CancellationTokenSource _linkedCts;

    private ServerTaskScheduler(CancellationToken externalToken)
    {
        _externalToken = externalToken;
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
    }

    public void Dispose() => _linkedCts.Dispose();

    public void Enqueue(Func<ServerTaskScheduler, CancellationToken, ValueTask> task) => _tasks.Add(EnqueueWrapper(task));

    public static async Task Run(Func<ServerTaskScheduler, CancellationToken, ValueTask> mainTask, CancellationToken cancellationToken = default)
    {
        using var scheduler = new ServerTaskScheduler(cancellationToken);
        scheduler.Enqueue(mainTask);
        await scheduler.Run().ConfigureAwait(false);
    }

    private async Task EnqueueWrapper(Func<ServerTaskScheduler, CancellationToken, ValueTask> taskFunc) => await taskFunc(this, _linkedCts.Token).ConfigureAwait(false);

    private async Task Run()
    {
        var exceptions = new List<Exception>();
        var runningTasks = new HashSet<Task>();
        while (!_tasks.IsEmpty || runningTasks.Count > 0)
        {
            while (_tasks.TryTake(out var task))
                runningTasks.Add(task);

            var completedTask = await Task.WhenAny(runningTasks).ConfigureAwait(false);
            runningTasks.Remove(completedTask);

            if (!completedTask.IsFaulted)
                continue;

            await _linkedCts.CancelAsync().ConfigureAwait(false);
            foreach (var exception in completedTask.Exception.InnerExceptions)
                exceptions.Add(exception);
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