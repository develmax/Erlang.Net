namespace Erlang.Tests;

internal static class CleanupStealingHostChecks
{
    public static async Task Run()
    {
        var runtime = new ProcessRuntime(CleanupStealingHostNames.Lanes);
        var blockerReady = NewGate();
        var observerReady = NewGate();
        var blockerCanceled = NewGate();
        var observerCanceled = NewGate();
        var resumeBlocker = NewGate();
        var resumeObserver = NewGate();
        var blockerEntered = NewGate();
        var releaseBlocker = NewGate();
        var observerEntered = NewGate();
        var failures = new List<Exception>();
        bool halted = false;
        _ = runtime.Spawn(async context =>
        {
            await Bind(context, false);
            TaskScheduler ownerScheduler = TaskScheduler.Current;
            using var cancellation = context.Cancellation.Register(() => blockerCanceled.TrySetResult());
            blockerReady.TrySetResult();
            await resumeBlocker.Task;
            Require(context.Cancellation.IsCancellationRequested);
            Require(ReferenceEquals(TaskScheduler.Current, ownerScheduler));
            blockerEntered.TrySetResult();
            Console.WriteLine(CleanupStealingHostNames.BlockerEntered);
            Console.Out.Flush();
            releaseBlocker.Task.GetAwaiter().GetResult();

            return Nil.Value;
        });
        _ = runtime.Spawn(async context =>
        {
            await Bind(context, true);
            TaskScheduler ownerScheduler = TaskScheduler.Current;
            using var cancellation = context.Cancellation.Register(() => observerCanceled.TrySetResult());
            observerReady.TrySetResult();
            await resumeObserver.Task;
            Require(context.Cancellation.IsCancellationRequested);
            Require(ReferenceEquals(TaskScheduler.Current, ownerScheduler));
            Require(blockerEntered.Task.IsCompleted && !releaseBlocker.Task.IsCompleted);
            Console.WriteLine(CleanupStealingHostNames.ObserverEntered);
            Console.Out.Flush();
            observerEntered.TrySetResult();

            return Nil.Value;
        });
        try
        {
            await Task.WhenAll(blockerReady.Task, observerReady.Task).WaitAsync(CleanupStealingHostNames.Timeout);
            Task<RuntimeHaltResult> selected = runtime.RequestImmediateHalt(CleanupStealingHostNames.HaltStatus);
            halted = true;
            Require((await selected.WaitAsync(CleanupStealingHostNames.Timeout)).Status == CleanupStealingHostNames.HaltStatus);
            await Task.WhenAll(blockerCanceled.Task, observerCanceled.Task).WaitAsync(CleanupStealingHostNames.Timeout);
            Require(!runtime.HaltQuiescence.IsCompleted);
            resumeBlocker.TrySetResult();
            await blockerEntered.Task.WaitAsync(CleanupStealingHostNames.Timeout);
            Require(!releaseBlocker.Task.IsCompleted && !runtime.HaltQuiescence.IsCompleted);
            resumeObserver.TrySetResult();
            await observerEntered.Task.WaitAsync(CleanupStealingHostNames.Timeout);
            Require(!releaseBlocker.Task.IsCompleted && !runtime.HaltQuiescence.IsCompleted);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            // Always release the real blocked actor before joining owned execution.
            resumeObserver.TrySetResult();
            resumeBlocker.TrySetResult();
            releaseBlocker.TrySetResult();
            try
            {
                if (!halted)
                    await runtime.RequestImmediateHalt(CleanupStealingHostNames.HaltStatus).WaitAsync(CleanupStealingHostNames.Timeout);
                await runtime.HaltQuiescence.WaitAsync(CleanupStealingHostNames.Timeout);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(CleanupStealingHostNames.Timeout);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0)
            throw new AggregateException(failures);
    }

    private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task Bind(ProcessContext context, bool unbind)
    {
        Term previous = await context.Runtime.Modules.Call(context, CleanupStealingHostNames.Module,
            CleanupStealingHostNames.ProcessFlag, Term.A(CleanupStealingHostNames.Scheduler), Term.I(CleanupStealingHostNames.BoundLane));
        Require(previous.Equals(Term.I(CleanupStealingHostNames.Unbound)));
        Term lane = await context.Runtime.Modules.Call(context, CleanupStealingHostNames.Module,
            CleanupStealingHostNames.SystemInfo, Term.A(CleanupStealingHostNames.SchedulerId));
        Require(lane.Equals(Term.I(CleanupStealingHostNames.BoundLane)));
        if (unbind)
        {
            Term bound = await context.Runtime.Modules.Call(context, CleanupStealingHostNames.Module,
                CleanupStealingHostNames.ProcessFlag, Term.A(CleanupStealingHostNames.Scheduler), Term.I(CleanupStealingHostNames.Unbound));
            Require(bound.Equals(Term.I(CleanupStealingHostNames.BoundLane)));
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(CleanupStealingHostNames.Failure);
    }
}
