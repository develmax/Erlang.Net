namespace Erlang.Tests;

internal static class NormalBarrier696Checks
{
    internal static async Task ActualTurnsAuxiliaryAndDirty()
    {
        await using var runtime = new ProcessRuntime(4);
        await runtime.InitializeModulesAsync(_ => { });
        await using var normal = new RuntimeNodeFoundationHostActor(runtime);
        await using var dirty = new RuntimeNodeFoundationHostActor(runtime);
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        await using var probe = new RuntimeNodeFoundationHostActor(runtime);
        using var normalEntered = new ManualResetEventSlim();
        using var normalRelease = new ManualResetEventSlim();
        using var dirtyEntered = new ManualResetEventSlim();
        using var dirtyRelease = new ManualResetEventSlim();
        var acquired = NewSignal();
        var queued = NewSignal();
        var releaseBarrier = NewSignal();
        Task? normalWork = null, dirtyWork = null, barrierWork = null, probeWork = null, auxiliaryWork = null;
        Task? physicalDirty = null;
        int actorCalls = 0, auxiliaryCalls = 0;
        try
        {
            normalWork = normal.InvokeAsync(async _ =>
            {
                normalEntered.Set();
                Require(normalRelease.Wait(NormalBarrier696Names.Deadline));
                await Task.CompletedTask;

                return true;
            });
            Require(normalEntered.Wait(NormalBarrier696Names.Deadline));
            dirtyWork = dirty.InvokeAsync(async context =>
            {
                object kind = Enum.Parse(typeof(ProcessRuntime).Assembly.GetType(DirtyNative419Names.Kind, true)!, DirtyNative419Names.Cpu);
                Func<ProcessContext, IReadOnlyList<Term>, Term> body = (owner, _) =>
                {
                    Require(!DirtyNative419Access.NormalTurn(owner));
                    Require(DirtyNative419Access.Kind(owner) == DirtyNative419Names.Cpu);
                    dirtyEntered.Set();
                    Require(dirtyRelease.Wait(NormalBarrier696Names.Deadline));

                    return Nil.Value;
                };
                Action<Task> retainActualSettlement = task => physicalDirty = task;
                object pending = typeof(ProcessRuntime).GetMethod(DirtyNative419Names.Schedule, DirtyNative419Names.Members)!
                    .Invoke(
                        runtime,
                        [context, kind, DirtyNative419Names.Runtime, DirtyNative419Names.Schedule,
                        Array.Empty<Term>(), body, retainActualSettlement]
                    )!;
                await (ValueTask<Term>)pending;

                return true;
            });
            Require(dirtyEntered.Wait(NormalBarrier696Names.Deadline));
            barrierWork = requester.InvokeAsync(async context =>
            {
                Task<IDisposable> pending = Acquire(context);
                queued.TrySetResult();
                try
                {
                    using IDisposable lease = await pending;
                    Require(DirtyNative419Access.NormalTurn(context));
                    acquired.TrySetResult();
                    await releaseBarrier.Task;
                    Require(DirtyNative419Access.NormalTurn(context));
                    throw new InvalidOperationException(NormalBarrier696Names.CallbackFailure);
                }
                catch (InvalidOperationException error) when (error.Message == NormalBarrier696Names.CallbackFailure)
                {
                    return true;
                }
            });
            await queued.Task.WaitAsync(NormalBarrier696Names.Deadline);
            Require(!acquired.Task.IsCompleted); // real other actor is physically blocked
            normalRelease.Set();
            await acquired.Task.WaitAsync(NormalBarrier696Names.Deadline);
            probeWork = probe.InvokeAsync(async _ =>
            {
                Interlocked.Increment(ref actorCalls);
                await Task.CompletedTask;

                return true;
            });
            auxiliaryWork = Auxiliary(runtime, _ => Interlocked.Increment(ref auxiliaryCalls));
            dirtyRelease.Set();
            Require(physicalDirty is not null);
            await physicalDirty!.WaitAsync(NormalBarrier696Names.Deadline);
            Require(!probeWork.IsCompleted && Volatile.Read(ref actorCalls) == 0);
            Require(!auxiliaryWork.IsCompleted && Volatile.Read(ref auxiliaryCalls) == 0);
            releaseBarrier.TrySetResult();
            await barrierWork.WaitAsync(NormalBarrier696Names.Deadline);
            await probeWork.WaitAsync(NormalBarrier696Names.Deadline);
            await auxiliaryWork.WaitAsync(NormalBarrier696Names.Deadline);
            Require(actorCalls == 1 && auxiliaryCalls == 4);
        }
        finally
        {
            normalRelease.Set();
            dirtyRelease.Set();
            releaseBarrier.TrySetResult();
            // Attempt every started operation independently; never strand a
            // real blocked actor/worker if the first assertion or join fails.
            var errors = new List<Exception>();
            foreach (Task? task in new[] { normalWork, dirtyWork, barrierWork, probeWork, auxiliaryWork })
                if (task is not null)
                    try
                    {
                        await task.WaitAsync(NormalBarrier696Names.Deadline);
                    }
                    catch (Exception error)
                    {
                        errors.Add(error);
                    }
            if (errors.Count != 0) throw new AggregateException(errors);
        }
    }

    internal static async Task ReentrantAndRetirement()
    {
        await using var runtime = new ProcessRuntime(4);
        await runtime.InitializeModulesAsync(_ => { });
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        await using var normal = new RuntimeNodeFoundationHostActor(runtime);
        await requester.InvokeAsync(async context =>
        {
            using IDisposable outer = await Acquire(context);
            bool rejected = false;
            try
            {
                using IDisposable nested = await Acquire(context);
            }
            catch (InvalidOperationException error) when (error.Message == NormalBarrier696Names.Reentrant)
            {
                rejected = true;
            }
            Require(rejected && DirtyNative419Access.NormalTurn(context));

            return true;
        });
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var queued = NewSignal();
        Task? blocked = null, pending = null;
        try
        {
            blocked = normal.InvokeAsync(async _ =>
            {
                entered.Set();
                Require(release.Wait(NormalBarrier696Names.Deadline));
                await Task.CompletedTask;

                return true;
            });
            Require(entered.Wait(NormalBarrier696Names.Deadline));
            pending = requester.InvokeAsync(async context =>
            {
                Task<IDisposable> wait = Acquire(context);
                queued.TrySetResult();
                bool retired = false;
                try
                {
                    using IDisposable lease = await wait;
                }
                catch (ObjectDisposedException)
                {
                    retired = true;
                }
                Require(retired);

                return true;
            });
            await queued.Task.WaitAsync(NormalBarrier696Names.Deadline);
            object dispatcher = Dispatcher(runtime);
            _ = (Task)dispatcher.GetType().GetMethod(NormalBarrier696Names.Retire, NormalBarrier696Names.Members)!.Invoke(dispatcher, null)!;
            // The pending TCS must settle even while another actual turn stays
            // blocked. Otherwise runtime completion can never reach idle.
            await pending.WaitAsync(NormalBarrier696Names.Deadline);
        }
        finally
        {
            release.Set();
            var failures = new List<Exception>();
            foreach (Task? task in new[] { blocked, pending })
                if (task is not null)
                    try
                    {
                        await task.WaitAsync(NormalBarrier696Names.Deadline);
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }
            if (failures.Count != 0) throw new AggregateException(failures);
        }
    }

    internal static async Task ConcurrentCancellation()
    {
        await using var runtime = new ProcessRuntime(4);
        await runtime.InitializeModulesAsync(_ => { });
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var second = new RuntimeNodeFoundationHostActor(runtime);
        await using var normal = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext firstContext = await first.Ready, secondContext = await second.Ready;
        using var normalEntered = new ManualResetEventSlim();
        using var normalRelease = new ManualResetEventSlim();
        using var firstEntered = new ManualResetEventSlim();
        using var secondEntered = new ManualResetEventSlim();
        using var publishBoth = new ManualResetEventSlim();
        var queuedFirst = NewSignal();
        var queuedSecond = NewSignal();
        var acquired = NewSignal();
        var release = NewSignal();
        Task? blocked = null, firstWork = null, secondWork = null;

        async Task<bool> Run(ProcessContext context, ManualResetEventSlim entered, TaskCompletionSource queued)
        {
            entered.Set();
            Require(publishBoth.Wait(NormalBarrier696Names.Deadline));
            Task<IDisposable> waiting = Acquire(context);
            queued.TrySetResult();
            try
            {
                using IDisposable lease = await waiting;
                acquired.TrySetResult();
                await release.Task;

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
        try
        {
            blocked = normal.InvokeAsync(async _ =>
            {
                normalEntered.Set();
                Require(normalRelease.Wait(NormalBarrier696Names.Deadline));
                await Task.CompletedTask;

                return true;
            });
            Require(normalEntered.Wait(NormalBarrier696Names.Deadline));
            firstWork = first.InvokeAsync(context => Run(context, firstEntered, queuedFirst));
            secondWork = second.InvokeAsync(context => Run(context, secondEntered, queuedSecond));
            Require(firstEntered.Wait(NormalBarrier696Names.Deadline));
            Require(secondEntered.Wait(NormalBarrier696Names.Deadline));
            publishBoth.Set();
            await Task.WhenAll(queuedFirst.Task, queuedSecond.Task).WaitAsync(NormalBarrier696Names.Deadline);
            object dispatcher = Dispatcher(runtime);
            object barrier = dispatcher.GetType().GetField(NormalBarrier696Names.Barrier, NormalBarrier696Names.Members)!.GetValue(dispatcher)!;
            object scheduler = barrier.GetType().GetProperty(NormalBarrier696Names.Scheduler, NormalBarrier696Names.Members)!.GetValue(barrier)!;
            var canceled = (ProcessContext)scheduler.GetType().GetProperty(NormalBarrier696Names.Owner, NormalBarrier696Names.Members)!.GetValue(scheduler)!;
            Require(ReferenceEquals(canceled, firstContext) || ReferenceEquals(canceled, secondContext));
            DirtyNative419Access.Cancel(canceled);
            Require(!acquired.Task.IsCompleted);
            normalRelease.Set();
            await acquired.Task.WaitAsync(NormalBarrier696Names.Deadline);
            release.TrySetResult();
            await Task.WhenAll(firstWork, secondWork).WaitAsync(NormalBarrier696Names.Deadline);
        }
        finally
        {
            normalRelease.Set();
            publishBoth.Set();
            release.TrySetResult();
            var failures = new List<Exception>();
            foreach (Task? task in new[] { blocked, firstWork, secondWork })
                if (task is not null)
                    try
                    {
                        await task.WaitAsync(NormalBarrier696Names.Deadline);
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }
            if (failures.Count != 0) throw new AggregateException(failures);
        }
    }

    private static async Task<IDisposable> Acquire(ProcessContext context)
    {
        object pending = typeof(ProcessRuntime).GetMethod(NormalBarrier696Names.Acquire, NormalBarrier696Names.Members)!
            .Invoke(context.Runtime, [context])!;
        var task = (Task)pending.GetType().GetMethod(NormalBarrier696Names.AsTask)!.Invoke(pending, null)!;
        await task;

        return (IDisposable)task.GetType().GetProperty(NormalBarrier696Names.Result)!.GetValue(task)!;
    }

    private static Task Auxiliary(ProcessRuntime runtime, Action<int> body)
    {
        object dispatcher = Dispatcher(runtime);
        object batch = dispatcher.GetType().GetMethod(NormalBarrier696Names.PrepareAuxiliary, NormalBarrier696Names.Members)!.Invoke(dispatcher, [body])!;
        batch.GetType().GetMethod(NormalBarrier696Names.Publish, NormalBarrier696Names.Members)!.Invoke(batch, null);

        return (Task)batch.GetType().GetProperty(NormalBarrier696Names.Completion, NormalBarrier696Names.Members)!.GetValue(batch)!;
    }

    private static object Dispatcher(ProcessRuntime runtime)
        => typeof(ProcessRuntime).GetField(NormalBarrier696Names.Dispatcher, NormalBarrier696Names.Members)!.GetValue(runtime)!;

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NormalBarrier696Names.Failure);
    }
}
