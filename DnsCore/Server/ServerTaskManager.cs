using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DnsCore.Server;

internal sealed class ServerTaskManager : IAsyncDisposable
{
    private readonly Channel<Task> _tasks = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public async ValueTask Add(Task task) => await _tasks.Writer.WriteAsync(task).ConfigureAwait(false);

    public void CompleteAdding() => _tasks.Writer.TryComplete();

    public async ValueTask DisposeAsync()
    {
        var waitForMoreTasks = _tasks.Reader.WaitToReadAsync().AsTask();
        List<Task> pendingTasks = [waitForMoreTasks];
        while (pendingTasks.Count > 0)
        {
            var task = await Task.WhenAny(pendingTasks).ConfigureAwait(false);
            if (task.IsFaulted)
                await task.ConfigureAwait(false); // We want to throw the exception

            pendingTasks.Remove(task);
            if (task != waitForMoreTasks)
                continue;

            if (!await waitForMoreTasks.ConfigureAwait(false))
                continue;

            while (_tasks.Reader.TryRead(out var newTask))
                pendingTasks.Add(newTask);

            waitForMoreTasks = _tasks.Reader.WaitToReadAsync().AsTask();
            pendingTasks.Add(waitForMoreTasks);
        }
    }
}
