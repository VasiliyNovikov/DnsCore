using System;
using System.Threading.Tasks;

namespace DnsCore.Utils;

public static class TaskExtensions
{
    public static async ValueTask<TTask> WhenCompleted<TTask>(this TTask task) where TTask : Task
    {
        try
        {
            await task.ConfigureAwait(false);
            return task;
        }
        catch
        {
            return task;
        }
    }

    public static async ValueTask<Task> WhenCompleted(this ValueTask task)
    {
        try
        {
            await task.ConfigureAwait(false);
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }
    }

    public static async ValueTask<Task<T>> WhenCompleted<T>(this ValueTask<T> task)
    {
        try
        {
            return Task.FromResult(await task.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            return Task.FromException<T>(e);
        }
    }
}