using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Tests;

namespace Erlang;

internal static class PollVmChecks1570
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(PollVmNames1570.Contract);
    }

    private static ProcessRuntime Create(RuntimeWorkerCreationPolicy? policy = null)
        => new(
            ProcessNodeDefaults.Local,
            TextWriter.Null,
            ProcessTableParameters.DefaultCapacity,
            null,
            PollVmNames1570.NormalWorkers,
            new ProcessRuntimeOptions { DirtyIoSchedulerCount = PollVmNames1570.DirtyIoWorkers },
            policy
        );

    private static LinuxDescriptorPollService Poll(ProcessRuntime runtime)
        => runtime.DescriptorPolling558 ?? throw new InvalidOperationException(PollVmNames1570.Contract);

    private static bool Contains(Exception error, Exception sentinel)
        => ReferenceEquals(error, sentinel)
            || error is AggregateException aggregate && aggregate.InnerExceptions.Any(item => Contains(item, sentinel))
            || error.InnerException is { } inner && Contains(inner, sentinel);

    private static bool OnlySentinel(Exception error, Exception sentinel)
        => ReferenceEquals(error, sentinel)
            || error is AggregateException { InnerExceptions.Count: > 0 } aggregate
                && aggregate.InnerExceptions.All(item => OnlySentinel(item, sentinel));

    private static void CheckLive(ProcessRuntime runtime)
    {
        var poll = Poll(runtime);
        Require(poll.Ready.IsCompletedSuccessfully && poll.ActualThreadId > 0
            && !poll.PhysicalLifetimeEnded && !poll.ActualThreadJoined);
        Require(WorkerLifecycleAccess.Read(poll, PollVmNames1570.Thread) is Thread { IsAlive: true });
        Require(Directory.Exists(PollVmNames1570.ProcTasks + poll.ActualThreadId));
        var accounting = runtime.Statistics.Microstates;
        lock (WorkerLifecycleAccess.Read(accounting, PollVmNames1570.RuntimeGate)!)
        {
            var records = (IDictionary)WorkerLifecycleAccess.Read(accounting, PollVmNames1570.AccountingRecords)!;
            RuntimeMicrostateRecord row = records.Values.Cast<RuntimeMicrostateRecord>()
                .Single(record => record.Type == PollVmNames1570.Poll);
            Require(row.Identity == PollVmNames1570.PollIdentity && !row.Managed && row.Active
                && ReferenceEquals(row.Owner, accounting) && row.PhysicalThread.OperatingSystemId == poll.ActualThreadId);
        }
    }

    private static void CheckClosed(LinuxDescriptorPollService poll, bool started)
    {
        Require(poll.PhysicalLifetimeEnded && poll.ActualThreadJoined == started
            && WorkerLifecycleAccess.Read(poll, PollVmNames1570.Thread) is Thread { IsAlive: false }
            && (int)WorkerLifecycleAccess.Read(poll, PollVmNames1570.WakeRead)! == PollVmNames1570.ClosedWakeFd
            && (int)WorkerLifecycleAccess.Read(poll, PollVmNames1570.WakeWrite)! == PollVmNames1570.ClosedWakeFd);
    }

    internal static async Task<object> Run(string mode)
    {
        if (mode is PollVmNames1570.BeforeStart or PollVmNames1570.BeforeAccounting or PollVmNames1570.Entered)
            return await CreationFailure(mode);
        var runtimes = new List<ProcessRuntime>(2);
        Exception? primary = null;
        var cleanup = new List<Exception>();
        object? result = null;
        try
        {
            ProcessRuntime first = Create();
            runtimes.Add(first);
            LinuxDescriptorPollService firstPoll = Poll(first);
            CheckLive(first);
            if (mode is PollVmNames1570.Eager or PollVmNames1570.Isolation)
            {
                ProcessRuntime second = Create();
                runtimes.Add(second);
                LinuxDescriptorPollService secondPoll = Poll(second);
                CheckLive(second);
                Require(!ReferenceEquals(firstPoll, secondPoll) && firstPoll.ActualThreadId != secondPoll.ActualThreadId
                    && !ReferenceEquals(first.Statistics.Microstates, second.Statistics.Microstates));
                int firstTid = firstPoll.ActualThreadId, secondTid = secondPoll.ActualThreadId;
                if (mode == PollVmNames1570.Isolation)
                {
                    await first.DisposeAsync().AsTask().WaitAsync(PollVmNames1570.Deadline);
                    CheckClosed(firstPoll, true);
                    CheckLive(second);
                    var actor = second.Spawn(_ => ValueTask.FromResult<Term>(Term.I(PollVmNames1570.LiveValue)));
                    Require((await actor.Completion.WaitAsync(PollVmNames1570.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
                    WorkerLifecycleAccess.RuntimeResources(first);
                }
                result = new
                {
                    TwoActualOwners = true,
                    FirstThread = firstTid,
                    SecondThread = secondTid,
                    ForeignVmStillLiveAfterDispose = mode == PollVmNames1570.Isolation
                };
            }
            else if (mode == PollVmNames1570.Halt)
                result = await Halt(first, firstPoll);
            else if (mode == PollVmNames1570.InitDeath)
            {
                var lifetime = (DescriptorPollRuntimeLifetime558)WorkerLifecycleAccess.Read(first, PollVmNames1570.Lifetime)!;
                var killer = first.Spawn(context =>
                {
                    first.Exit(context, first.InitializationPid, Term.A(ProcessExitSignals.Kill));

                    return ValueTask.FromResult<Term>(Nil.Value);
                });
                _ = await killer.Completion.WaitAsync(PollVmNames1570.Deadline);
                await lifetime.Completion.WaitAsync(PollVmNames1570.Deadline);
                Require(lifetime.CloseRequested && !first.IsAlive(first.InitializationPid));
                CheckClosed(firstPoll, true);
                result = new
                {
                    ActualInitializerDeath = true,
                    OwnedPollCompletionJoined = true,
                    ProofBeforeAnyHostDispose = true
                };
            }
            else throw new ArgumentOutOfRangeException(nameof(mode));
        }
        catch (Exception error)
        {
            primary = error;
        }
        finally
        {
            foreach (ProcessRuntime runtime in runtimes)
            {
                try
                {
                    await runtime.DisposeAsync().AsTask().WaitAsync(PollVmNames1570.Deadline);
                }
                catch (Exception error)
                {
                    cleanup.Add(error);
                }
            }
        }
        if (primary is not null || cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            if (cleanup.Count == 1) ExceptionDispatchInfo.Capture(cleanup[0]).Throw();
            throw new AggregateException(cleanup);
        }
        foreach (ProcessRuntime runtime in runtimes) CheckClosed(Poll(runtime), true);

        return new { Mode = mode, Evidence = result, AllOwnedPollersJoined = true };
    }

    private static async Task<object> Halt(ProcessRuntime runtime, LinuxDescriptorPollService poll)
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        ProcessHandle? actor = null;
        Exception? primary = null, cleanup = null;
        bool logicalBeforeRelease = false;
        try
        {
            actor = runtime.Spawn(_ =>
            {
                entered.Set();
                release.Wait();

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            Require(entered.Wait(PollVmNames1570.Deadline));
            RuntimeHaltResult halt = await runtime.RequestImmediateHalt(PollVmNames1570.HaltStatus).WaitAsync(PollVmNames1570.Deadline);
            Require(halt.Status == PollVmNames1570.HaltStatus && !runtime.HaltQuiescence.IsCompleted);
            logicalBeforeRelease = true;
        }
        catch (Exception error)
        {
            primary = error;
        }
        finally
        {
            release.Set();
            if (actor is not null)
            {
                try
                {
                    _ = await actor.Completion.WaitAsync(PollVmNames1570.Deadline);
                }
                catch (Exception error)
                {
                    cleanup = error;
                }
            }
        }
        if (primary is not null && cleanup is not null) throw new AggregateException(primary, cleanup);
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
        if (cleanup is not null) ExceptionDispatchInfo.Capture(cleanup).Throw();
        await runtime.HaltQuiescence.WaitAsync(PollVmNames1570.Deadline);
        CheckClosed(poll, true);

        return new
        {
            NoFlushLogicalResultBeforeActorRelease = logicalBeforeRelease,
            PhysicalQuiescenceIncludesPollJoin = true
        };
    }

    private static async Task<object> CreationFailure(string mode)
    {
        ProcessRuntime? captured = null;
        LinuxDescriptorPollService? service = null;
        var sentinel = new InvalidOperationException(PollVmNames1570.Sentinel + mode);
        int hookCalls = 0, hookThread = 0;
        RuntimeMicrostateRecord? actualRecord = null;

        void Hook(string kind, int identity, string stage)
        {
            if (kind != RuntimeWorkerCreationNames.Poll || stage != mode) return;
            Require(identity == PollVmNames1570.PollIdentity && captured is not null);
            service = Poll(captured!);
            hookThread = RuntimeOsThreadIdentity.Current();
            Interlocked.Increment(ref hookCalls);
            if (mode == PollVmNames1570.Entered)
            {
                actualRecord = RuntimeMicrostateExecution.Current?.Record;
                Require(actualRecord is not null && actualRecord.Type == PollVmNames1570.Poll
                    && actualRecord.Identity == PollVmNames1570.PollIdentity && !actualRecord.Managed
                    && ReferenceEquals(actualRecord.Owner, captured!.Statistics.Microstates)
                    && ReferenceEquals(actualRecord.PhysicalThread, RuntimeMicrostateThreadIdentity.Current));
            }
            throw sentinel;
        }
        var policy = new RuntimeWorkerCreationPolicy(
            (kind, identity) => Hook(kind, identity, PollVmNames1570.BeforeStart),
            (kind, identity) => Hook(kind, identity, PollVmNames1570.Entered),
            (_, owner) =>
 {
     if (owner is ProcessRuntime actual) captured = actual;
 },
            (kind, identity) => Hook(kind, identity, PollVmNames1570.BeforeAccounting)
        );
        Exception? observed = null;
        ProcessRuntime? unexpected = null;
        try
        {
            unexpected = Create(policy);
        }
        catch (Exception error)
        {
            observed = error;
        }
        if (unexpected is not null)
        {
            await unexpected.DisposeAsync().AsTask().WaitAsync(PollVmNames1570.Deadline);
            throw new InvalidOperationException(PollVmNames1570.Contract);
        }
        Require(observed is not null && Contains(observed, sentinel) && OnlySentinel(observed, sentinel)
            && captured is not null && service is not null && hookCalls == 1 && hookThread > 0);
        bool started = mode != PollVmNames1570.BeforeStart;
        CheckClosed(service!, started);
        WorkerLifecycleAccess.RuntimeResources(captured!);
        Require(mode != PollVmNames1570.Entered || actualRecord is { Active: false });

        return new
        {
            Mode = mode,
            ExactSentinelRetained = true,
            UnrelatedFailureRejected = true,
            HookCalls = hookCalls,
            ActualHookThreadId = hookThread,
            ActualPollThreadJoined = service!.ActualThreadJoined,
            BeforeStartProvenUnstarted = !started,
            PhysicalWakeResourcesClosed = true,
            ProofAtConstructorThrowBeforeHostDispose = true,
            OriginalFailure = observed!.ToString()
        };
    }
}
