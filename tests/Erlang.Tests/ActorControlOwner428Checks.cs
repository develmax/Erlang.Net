using System.Reflection;

namespace Erlang.Tests;

internal static class ActorControlOwner428Checks
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(ActorControlOwner428Names.Failure);
    }

    private static bool TryAcquire(ProcessRuntime runtime, ProcessContext target, out IDisposable? lease)
    {
        object dispatcher = typeof(ProcessRuntime).GetField(ActorControlOwner428Names.DispatcherField,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        object?[] args = [target, null];
        bool accepted = (bool)dispatcher.GetType().GetMethod(ActorControlOwner428Names.AcquireMethod,
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(dispatcher, args)!;
        lease = (IDisposable?)args[1];

        return accepted;
    }

    internal static async Task Exclusion()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext target = await actor.Ready;
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<bool>? running = null;
        IDisposable? lease = null;
        try
        {
            running = actor.Invoke(_ =>
            {
                entered.Set();
                Require(release.Wait(ActorControlOwner428Names.Deadline));

                return true;
            });
            Require(entered.Wait(ActorControlOwner428Names.Deadline));
            Require(!TryAcquire(runtime, target, out var whileRunning) && whileRunning is null);
            release.Set();
            Require(await running);
            using var timeout = new CancellationTokenSource(ActorControlOwner428Names.Deadline);
            while (!TryAcquire(runtime, target, out lease))
            {
                timeout.Token.ThrowIfCancellationRequested();
                await Task.Yield();
            }
            Require(!TryAcquire(runtime, target, out var duplicate) && duplicate is null);
            bool executed = false;
            Task<bool> pending = actor.Invoke(_ =>
 {
     executed = true;

     return true;
 });
            // Public command acceptance plus one yield alone would not prove a queued
            // dispatcher continuation; inspect the real pending queue for this owner.
            object scheduler = typeof(ProcessContext).GetProperty(ActorControlOwner428Names.SchedulerProperty,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
            object queue = scheduler.GetType().GetProperty(ActorControlOwner428Names.PendingProperty,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(scheduler)!;
            var count = queue.GetType().GetProperty(nameof(IReadOnlyCollection<object>.Count))!;
            while ((int)count.GetValue(queue)! == 0)
            {
                timeout.Token.ThrowIfCancellationRequested();
                await Task.Yield();
            }
            Require(!executed && !pending.IsCompleted);
            lease!.Dispose();
            lease.Dispose();
            lease = null;
            Require(await pending && executed);
        }
        finally
        {
            lease?.Dispose();
            release.Set();
            if (running is not null) await running;
        }
    }
}
