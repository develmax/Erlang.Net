namespace Erlang.Tests;

// Component controls for real dispatcher/typed admission. Physical NIF branch
// integration is tested separately; these do not claim execution of a DLL.
internal static class NormalNative697Checks
{
    internal static async Task CountedHostAndSameContext()
    {
        await using var runtime = new ProcessRuntime(4);
        await runtime.InitializeModulesAsync(_ => { });
        await using var host = new RuntimeNodeFoundationHostActor(runtime);
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext hostContext = await host.Ready;
        using var held = new ManualResetEventSlim();
        using var releaseHost = new ManualResetEventSlim();
        var queuedBarrier = Signal();
        var acquiredBarrier = Signal();
        var releaseBarrier = Signal();
        Task? hostWork = null, barrierWork = null, impersonator = null;
        int entered = 0;
        Exception? primary = null;
        try
        {
            hostWork = Task.Run(() =>
            {
                using var admission = NormalNative697Access.Enter(hostContext);
                held.Set();
                NormalNative697Access.Require(releaseHost.Wait(NormalNative697Names.Deadline));
            });
            NormalNative697Access.Require(held.Wait(NormalNative697Names.Deadline));
            NormalNative697Access.Require(NormalNative697Access.Counts(runtime) == (1, 0));
            barrierWork = requester.InvokeAsync(async context =>
            {
                Task<IDisposable> wait = NormalNative697Access.Barrier(context);
                queuedBarrier.TrySetResult();
                using IDisposable barrier = await wait;
                using (NormalNative697Access.Enter(context))
                    NormalNative697Access.Require(NormalNative697Access.Counts(runtime) == (0, 0));
                // Task.Run copies this actor's ExecutionContext, but never its
                // actual ThreadStatic turn. SAME context must still be queued.
                impersonator = Task.Run(() =>
                {
                    using var admission = NormalNative697Access.Enter(context);
                    Interlocked.Increment(ref entered);
                });
                acquiredBarrier.TrySetResult();
                await releaseBarrier.Task;

                return true;
            });
            await queuedBarrier.Task.WaitAsync(NormalNative697Names.Deadline);
            NormalNative697Access.Require(!acquiredBarrier.Task.IsCompleted);
            releaseHost.Set();
            await acquiredBarrier.Task.WaitAsync(NormalNative697Names.Deadline);
            await NormalNative697Access.WaitCounts(runtime, 0, 1);
            NormalNative697Access.Require(entered == 0 && !impersonator!.IsCompleted);
            releaseBarrier.TrySetResult();
            await Task.WhenAll(hostWork, barrierWork, impersonator!).WaitAsync(NormalNative697Names.Deadline);
            NormalNative697Access.Require(entered == 1 && NormalNative697Access.Counts(runtime) == (0, 0));
        }
        catch (Exception failure)
        {
            primary = failure;
            throw;
        }
        finally
        {
            releaseHost.Set();
            releaseBarrier.TrySetResult();
            await Settle(
                primary,
                hostWork,
                barrierWork,
                impersonator
            );
        }
    }

    internal static async Task PendingAuxiliaryContinuation()
    {
        await using var runtime = new ProcessRuntime(4);
        await runtime.InitializeModulesAsync(_ => { });
        await using var host = new RuntimeNodeFoundationHostActor(runtime);
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await host.Ready;
        using var auxiliaryEntered = new ManualResetEventSlim();
        using var releaseAuxiliary = new ManualResetEventSlim();
        var queued = Signal();
        var granted = Signal();
        var releaseBarrier = Signal();
        Task? auxiliary = null, barrier = null;
        int first = 0, nativeEntries = 0;
        Exception? primary = null;
        try
        {
            auxiliary = NormalNative697Access.Auxiliary(
                runtime,
                _ =>
            {
                if (Interlocked.Increment(ref first) != 1) return;
                auxiliaryEntered.Set();
                NormalNative697Access.Require(releaseAuxiliary.Wait(NormalNative697Names.Deadline));
                // This genuine auxiliary awaits a continuation on another real
                // managed thread. Queue-on-PENDING creates a drain cycle.
                Task.Run(() =>
                {
                    using var entry = NormalNative697Access.Enter(context);
                    Interlocked.Increment(ref nativeEntries);
                }).GetAwaiter().GetResult();
            }
            );
            NormalNative697Access.Require(auxiliaryEntered.Wait(NormalNative697Names.Deadline));
            barrier = requester.InvokeAsync(async owner =>
            {
                Task<IDisposable> waiting = NormalNative697Access.Barrier(owner);
                queued.TrySetResult();
                using var entry = await waiting;
                granted.TrySetResult();
                await releaseBarrier.Task;

                return true;
            });
            await queued.Task.WaitAsync(NormalNative697Names.Deadline);
            NormalNative697Access.Require(!granted.Task.IsCompleted);
            releaseAuxiliary.Set();
            await granted.Task.WaitAsync(NormalNative697Names.Deadline);
            NormalNative697Access.Require(nativeEntries == 1);
        }
        catch (Exception failure)
        {
            primary = failure;
            throw;
        }
        finally
        {
            releaseAuxiliary.Set();
            releaseBarrier.TrySetResult();
            // A negative control must also release a queued off-turn entry.
            DirtyNative419Access.Cancel(context);
            await Settle(primary, auxiliary, barrier);
        }
    }

    internal static async Task QueuedCancellationAndEarlyClose()
    {
        await using var runtime = new ProcessRuntime(4);
        await runtime.InitializeModulesAsync(_ => { });
        await using var canceledHost = new RuntimeNodeFoundationHostActor(runtime);
        await using var closedHost = new RuntimeNodeFoundationHostActor(runtime);
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext canceledContext = await canceledHost.Ready, closedContext = await closedHost.Ready;
        var acquired = Signal();
        var release = Signal();
        Task? barrier = null, canceled = null, closed = null;
        Exception? primary = null;
        try
        {
            barrier = requester.InvokeAsync(async context =>
            {
                using var lease = await NormalNative697Access.Barrier(context);
                acquired.TrySetResult();
                await release.Task;

                return true;
            });
            await acquired.Task.WaitAsync(NormalNative697Names.Deadline);
            canceled = Task.Run(() =>
            {
                bool failed = false;
                try
                {
                    using var lease = NormalNative697Access.Enter(canceledContext);
                }
                catch (OperationCanceledException)
                {
                    failed = true;
                }
                NormalNative697Access.Require(failed);
            });
            await NormalNative697Access.WaitCounts(runtime, 0, 1);
            DirtyNative419Access.Cancel(canceledContext);
            await canceled.WaitAsync(NormalNative697Names.Deadline);
            closed = Task.Run(() =>
            {
                bool failed = false;
                try
                {
                    using var lease = NormalNative697Access.Enter(closedContext);
                }
                catch (ObjectDisposedException)
                {
                    failed = true;
                }
                NormalNative697Access.Require(failed);
            });
            await NormalNative697Access.WaitCounts(runtime, 0, 1);
            await NormalNative697Access.Close(runtime).WaitAsync(NormalNative697Names.Deadline);
            await closed.WaitAsync(NormalNative697Names.Deadline);
            // Early NIF shutdown has settled Host admission while the actual
            // exclusive owner remains held; late dispatcher close cannot fake it.
            NormalNative697Access.Require(!barrier.IsCompleted && NormalNative697Access.Counts(runtime) == (0, 0));
        }
        catch (Exception failure)
        {
            primary = failure;
            throw;
        }
        finally
        {
            release.TrySetResult();
            await Settle(
                primary,
                barrier,
                canceled,
                closed
            );
        }
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task Settle(Exception? primary, params Task?[] tasks)
    {
        var failures = new List<Exception>();
        foreach (var task in tasks)
            if (task is not null)
                try
                {
                    await task.WaitAsync(NormalNative697Names.Deadline);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
        if (failures.Count != 0)
        {
            if (primary is not null) failures.Insert(0, primary);
            throw new AggregateException(failures);
        }
    }
}
