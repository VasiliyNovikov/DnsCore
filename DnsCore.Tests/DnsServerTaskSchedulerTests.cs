using System;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsServerTaskSchedulerTests
{
    [TestMethod]
    public async Task Run_ExecutesSingleTaskSuccessfully()
    {
        var executed = false;
        await ServerTaskScheduler.Run(async (_, _) =>
        {
            await Task.Yield();
            executed = true;
        });
        Assert.IsTrue(executed);
    }

    [TestMethod]
    public async Task Run_TaskThrowsException_CapturesException()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            ServerTaskScheduler.Run(async (_, _) =>
            {
                await Task.Yield();
                throw new InvalidOperationException();
            }));
    }

    [TestMethod]
    public async Task Run_MultipleTasks_CancelsOnException()
    {
        var secondTaskExecuted = false;

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await ServerTaskScheduler.Run(async (scheduler, _) =>
            {
                scheduler.Enqueue(async (_, _) =>
                {
                    await Task.Yield();
                    throw new InvalidOperationException();
                });

                scheduler.Enqueue(async (_, ct) =>
                {
                    await Task.Delay(100, ct);
                    secondTaskExecuted = true;
                });

                await Task.Yield();
            }));

        Assert.IsFalse(secondTaskExecuted);
    }

    [TestMethod]
    public async Task Run_CancellationRequested_StopsTaskExecution()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var executed = false;

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await ServerTaskScheduler.Run(async (_, ct) =>
            {
                await Task.Delay(1000, ct);
                executed = true;
            }, cts.Token));

        Assert.IsFalse(executed);
    }
}