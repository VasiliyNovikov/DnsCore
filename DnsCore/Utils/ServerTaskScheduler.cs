using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DnsCore.Utils;

internal class ServerTaskScheduler
{
    private readonly ChannelWriter<Task> _tasks;
    private readonly CancellationToken _cancellationToken;

    private ServerTaskScheduler(ChannelWriter<Task> tasks, CancellationToken cancellationToken)
    {
        _tasks = tasks;
        _cancellationToken = cancellationToken;
    }

    public async ValueTask Enqueue(Func<ServerTaskScheduler, CancellationToken, ValueTask> task)
    {
        if (_cancellationToken.IsCancellationRequested)
            return;

        Task scheduledTask;
        try
        {
            scheduledTask = task(this, _cancellationToken).AsTask();
        }
        catch (Exception e)
        {
            scheduledTask = Task.FromException(e);
        }

        try
        {
            await _tasks.WriteAsync(scheduledTask).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            // Ignore
        }
    }

    public static async Task Run(Func<ServerTaskScheduler, CancellationToken, ValueTask> startTask, CancellationToken cancellationToken)
    {
        var tasks = Channel.CreateUnbounded<Task>();
        using var stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var scheduler = new ServerTaskScheduler(tasks.Writer, stopCancellation.Token);
        HashSet<Task> runningTasks = [startTask(scheduler, stopCancellation.Token).AsTask()];
        var exceptions = new List<Exception>();
        var waitToReadTask = Task.CompletedTask;
        while (true)
        {
            while (tasks.Reader.TryRead(out var task))
                runningTasks.Add(task);

            if (!stopCancellation.IsCancellationRequested && waitToReadTask.IsCompleted)
            {
                waitToReadTask = tasks.Reader.WaitToReadAsync(stopCancellation.Token).AsTask();
                runningTasks.Add(waitToReadTask);
            }

            if (runningTasks.Count == 0)
                break;

            var completedTask = await Task.WhenAny(runningTasks).ConfigureAwait(false);
            runningTasks.Remove(completedTask);

            if (!completedTask.IsFaulted && !cancellationToken.IsCancellationRequested)
                continue;

            if (completedTask.IsFaulted)
                exceptions.AddRange(completedTask.Exception.Flatten().InnerExceptions);

            if (stopCancellation.IsCancellationRequested)
                continue;

            tasks.Writer.Complete();
            await stopCancellation.CancelAsync().ConfigureAwait(false);
        }

        switch (exceptions.Count)
        {
            case 0:
                cancellationToken.ThrowIfCancellationRequested();
                break;
            case 1:
                ExceptionDispatchInfo.Throw(exceptions[0]);
                break;
            default:
                throw new AggregateException(exceptions);
        }
    }
}