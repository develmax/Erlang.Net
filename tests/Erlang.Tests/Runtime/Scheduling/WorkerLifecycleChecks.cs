using static Erlang.Tests.WorkerLifecycleAccess;

namespace Erlang.Tests;

internal static class WorkerLifecycleChecks
{
    internal static async Task PartialNormal()
    {
        foreach (bool beforeStart in new[] { false, true })
        {
            object? dispatcher = null;
            var probe = new WorkerLifecycleProbe();
            string marker = beforeStart ? WorkerLifecycleNames.NormalBeforeStart : WorkerLifecycleNames.NormalSecond;

            void FailSecond()
            {
                object first = ((Array)Read(dispatcher, "normalWorkers")!).GetValue(0)!;
                Ready(first, false).WaitAsync(WorkerLifecycleNames.Deadline).GetAwaiter().GetResult();
                throw new InvalidOperationException(marker);
            }
            object policy = Policy(
                start: (kind, identity) =>
            {
                if (beforeStart && identity == 2)
                    FailSecond();
            },
                entered: (kind, identity) =>
            {
                probe.Entered(kind, identity);
                if (!beforeStart && identity == 2)
                    FailSecond();
            }
            );
            dispatcher = Create(
                WorkerLifecycleNames.Dispatcher,
                2,
                null,
                null,
                true,
                policy
            );
            await using var cleanup = new WorkerLifecycleOwner(dispatcher, false);
            await MustFail(Task.Run(() => Call(dispatcher, "StartNormalWorkers")), marker);
            await MustFail((Task)Call(dispatcher, "CompleteWhenIdle"), marker);
            Require(probe.Count == (beforeStart ? 1 : 2));
            Require(Ready(((Array)Read(dispatcher, "normalWorkers")!).GetValue(0)!, false).IsCompletedSuccessfully);
            if (beforeStart)
                Require(!Ready(((Array)Read(dispatcher, "normalWorkers")!).GetValue(1)!, false).IsCompleted);
            Joined(dispatcher, false);
            probe.AssertPhysicallyExited();
        }
    }

    internal static async Task PartialDirty()
    {
        foreach (bool beforeStart in new[] { false, true })
        {
            object? owner = null;
            var probe = new WorkerLifecycleProbe();
            string marker = beforeStart ? WorkerLifecycleNames.DirtyBeforeStart : WorkerLifecycleNames.DirtySecond;

            void FailSecond()
            {
                Array workers = (Array)Read(owner, "workers522")!;
                Ready(workers.GetValue(0)!, true).WaitAsync(WorkerLifecycleNames.Deadline).GetAwaiter().GetResult();
                Ready(workers.GetValue(1)!, true).WaitAsync(WorkerLifecycleNames.Deadline).GetAwaiter().GetResult();
                throw new InvalidOperationException(marker);
            }
            object policy = Policy(
                start: (kind, identity) =>
            {
                if (beforeStart && kind == WorkerLifecycleNames.DirtyIo && identity == 2)
                    FailSecond();
            },
                entered: (kind, identity) =>
            {
                probe.Entered(kind, identity);
                if (!beforeStart && kind == WorkerLifecycleNames.DirtyIo && identity == 2)
                    FailSecond();
            }
            );
            owner = Create(
                WorkerLifecycleNames.DirtyOwner,
                2,
                null,
                policy
            );
            await using var cleanup = new WorkerLifecycleOwner(owner, true);
            await MustFail(Task.Run(async () => await (Task)Call(owner, "EnsureWallTimeReady", CancellationToken.None)), marker);
            await MustFail((Task)Call(owner, "BeginClose"), marker);
            Require(probe.Count == (beforeStart ? 2 : 3));
            if (beforeStart)
                Require(!Ready(((Array)Read(owner, "workers522")!).GetValue(2)!, true).IsCompleted);
            Joined(owner, true);
            probe.AssertPhysicallyExited();
        }
    }

    internal static async Task Constructor()
    {
        foreach (string phase in new[]
        {
            WorkerLifecycleNames.PrefixBeforeDispatcher,
            WorkerLifecycleNames.PrefixBeforeAtoms,
            WorkerLifecycleNames.PrefixAfterStreams,
            WorkerLifecycleNames.BeforeInitializer,
            WorkerLifecycleNames.BeforeClrRegistration
        })
        {
            object? retained = null;
            var probe = new WorkerLifecycleProbe();
            object policy = Policy(
                entered: probe.Entered,
                checkpoint: (actual, owner) =>
            {
                if (actual != phase)
                    return;
                retained = owner;
                throw new InvalidOperationException(WorkerLifecycleNames.ConstructorError + phase);
            }
            );
            await MustFail(
                Task.Run(() => Create(
                    typeof(ProcessRuntime).FullName!,
                    ProcessNodeDefaults.Local,
                    null,
                    32768,
                    null,
                    2,
                    new ProcessRuntimeOptions { DirtyIoSchedulerCount = 2 },
                    policy
                )),
                WorkerLifecycleNames.ConstructorError + phase
            );
            Require(retained is not null);
            bool beforeWorkers = phase is WorkerLifecycleNames.PrefixBeforeDispatcher
                or WorkerLifecycleNames.PrefixBeforeAtoms or WorkerLifecycleNames.PrefixAfterStreams;
            Require(probe.Count == (beforeWorkers ? 0 : 5));
            RuntimeResources(retained!);
            probe.AssertPhysicallyExited();
        }
    }

    internal static async Task ConcurrentClose()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var probe = new WorkerLifecycleProbe();
        object policy = Policy(entered: (kind, identity) =>
        {
            probe.Entered(kind, identity);
            if (identity == 1)
            {
                entered.Set();
                Require(release.Wait(WorkerLifecycleNames.Deadline));
            }
        });
        object dispatcher = Create(
            WorkerLifecycleNames.Dispatcher,
            2,
            null,
            null,
            true,
            policy
        );
        await using var cleanup = new WorkerLifecycleOwner(dispatcher, false);
        Task startup = Task.Run(() => Call(dispatcher, "StartNormalWorkers"));
        Task? close = null;
        try
        {
            Require(entered.Wait(WorkerLifecycleNames.Deadline));
            object first = ((Array)Read(dispatcher, "normalWorkers")!).GetValue(0)!;
            Require(!Ready(first, false).IsCompleted);
            close = (Task)Call(dispatcher, "CompleteWhenIdle");
            Require(!close.IsCompleted);
        }
        finally
        {
            release.Set();
        }
        await MustFail(startup, nameof(ObjectDisposedException));
        await MustFail(close!, nameof(ObjectDisposedException));
        Require(!(bool)Read(dispatcher, "normalWorkersStarted")!);
        Require(probe.Count == 2);
        Joined(dispatcher, false);
        probe.AssertPhysicallyExited();
    }

    internal static async Task IndependentErrors()
    {
        var probe = new WorkerLifecycleProbe();
        object policy = Policy(entered: (kind, identity) =>
        {
            probe.Entered(kind, identity);
            throw new InvalidOperationException(identity == 1
                ? WorkerLifecycleNames.NormalFirst : WorkerLifecycleNames.NormalSecond);
        });
        object dispatcher = Create(
            WorkerLifecycleNames.Dispatcher,
            2,
            null,
            null,
            true,
            policy
        );
        await using var cleanup = new WorkerLifecycleOwner(dispatcher, false);
        await MustFail(Task.Run(() => Call(dispatcher, "StartNormalWorkers")),
            WorkerLifecycleNames.NormalFirst, WorkerLifecycleNames.NormalSecond);
        await MustFail((Task)Call(dispatcher, "CompleteWhenIdle"),
            WorkerLifecycleNames.NormalFirst, WorkerLifecycleNames.NormalSecond);
        Require(probe.Count == 2);
        Joined(dispatcher, false);
        probe.AssertPhysicallyExited();
    }
}
