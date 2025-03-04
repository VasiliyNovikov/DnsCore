using System;
using System.Threading.Tasks;

using DnsCore.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class TaskExtensionsTests
{
    [TestMethod]
    public async Task WhenCompleted_Exception()
    {
        try
        {
            throw new ArgumentException();
        }
        catch (Exception e)
        {
            var task = await Task.FromException(e).WhenCompleted();
            await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await task);
        }
    }

    [TestMethod]
    public async Task WhenCompleted_Waits()
    {
        var taskSource = new TaskCompletionSource();
        var waitingTask = taskSource.Task.WhenCompleted().AsTask();
        Assert.IsFalse(waitingTask.IsCompleted);
        taskSource.SetResult();
        Assert.IsTrue(waitingTask.IsCompleted);
        var task = await waitingTask;
        Assert.IsTrue(task.IsCompleted);
        await task;
    }
}