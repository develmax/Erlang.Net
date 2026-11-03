using System.Collections;
using System.Reflection;
using Erlang;

namespace Erlang.NifNativeBackend;

internal static class MicrostateSourceTailLifecycleChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(MicrostateSourceTailCaseNames.Contract);
    }

    private static int RegistryCount(RuntimeActorDispatcher dispatcher)
    {
        object gate = typeof(RuntimeActorDispatcher).GetField(MicrostateSourceTailCaseNames.Gate, Members)!.GetValue(dispatcher)!;
        lock (gate)
            return ((IEnumerable)typeof(RuntimeActorDispatcher).GetField(MicrostateSourceTailCaseNames.Registry, Members)!
                .GetValue(dispatcher)!).Cast<object>().Count();
    }

    private static bool Contains(Exception? actual, Exception expected)
    {
        if (ReferenceEquals(actual, expected)) return true;
        if (actual is AggregateException aggregate) return aggregate.InnerExceptions.Any(error => Contains(error, expected));

        return actual?.InnerException is { } inner && Contains(inner, expected);
    }

    private static async Task WaitUntil(Func<bool> predicate)
    {
        long until = Environment.TickCount64 + (long)MicrostateSourceTailCaseNames.Deadline.TotalMilliseconds;
        while (!predicate())
        {
            if (Environment.TickCount64 >= until) throw new TimeoutException(MicrostateSourceTailCaseNames.Contract);
            await Task.Delay(1).ConfigureAwait(false);
        }
    }

    internal static async Task<object> Run(string mode)
    {
        // One genuine VM at a time; no forged worker, turn or callback flags.
        var errors = new Exception?[MicrostateSourceTailCaseNames.CleanupCapacity];
        int errorCount = 0;
        Exception? primary = null, actorObserved = null;
        var ready = new TaskCompletionSource<MicrostateSourceTailPrepared>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var sentinel = new InvalidOperationException(MicrostateSourceTailCaseNames.Sentinel);
        ProcessRuntime? runtime = null;
        ProcessHandle? actor = null;
        MicrostateSourceTailPrepared? prepared = null;
        Task? closing = null;
        int ownCalls = 0, finishCalls = 0, helperRows = 0, expectedRows = 0;
        byte[]? helperEtf = null;
        bool actualPhysicalSource = false, tailFailureInspected = false;
        bool ownedShutdownCancellationInspected = false;
        bool gracefulShutdownMode = mode is MicrostateSourceTailCaseNames.WithheldClose or MicrostateSourceTailCaseNames.RunningClose;
        Exception? inspectedTailFailure = null;
        CancellationToken inspectedRequesterCancellation = default;
        try
        {
            runtime = new ProcessRuntime(MicrostateSourceTailCaseNames.NormalWorkers);
            ProcessRuntime actualRuntime = runtime;
            actor = runtime.Spawn(async context =>
            {
                try
                {
                    using StatisticsTrapAdmission source = await actualRuntime.Modules.AdmitStatisticsTrap(
                        context, StatisticsTrapNames.GatherMicrostates, MicrostateSourceTailCaseNames.GatherArity);
                    var dispatcher = (RuntimeActorDispatcher)typeof(ProcessRuntime)
                        .GetField(MicrostateSourceTailCaseNames.Dispatcher, Members)!.GetValue(actualRuntime)!;
                    int sourceLane = RuntimeActorExecutionTurn.LaneIdentity;
                    object sourceThread = RuntimeMicrostateThreadIdentity.Current;
                    RuntimeMicrostateRequest request = actualRuntime.Statistics.Microstates.CreateRequest(
                        RuntimeMicrostateAction.Gather, actualRuntime.ActorDispatchCapacity);
                    await using var trap = new StatisticsTrapRequest(actualRuntime, context);
                    RuntimeSchedulerAuxiliaryBatch420 batch = dispatcher.PrepareAuxiliary420(lane =>
                    {
                        if (lane == sourceLane)
                        {
                            Interlocked.Increment(ref ownCalls);
                            RuntimeMicrostateRecord? current = RuntimeMicrostateExecution.Current?.Record;
                            actualPhysicalSource = RuntimeNormalAuxiliaryTurn536.IsRuntime536(actualRuntime)
                                && RuntimeNormalAuxiliaryTurn536.Lane536 == sourceLane
                                && current is not null && current.Managed
                                && current.Identity == sourceLane
                                && ReferenceEquals(current.Owner, actualRuntime.Statistics.Microstates)
                                && ReferenceEquals(current.PhysicalThread, sourceThread)
                                && ReferenceEquals(current.PhysicalThread, RuntimeMicrostateThreadIdentity.Current);
                            Require(actualPhysicalSource);
                            entered.Set();
                            if (mode == MicrostateSourceTailCaseNames.RunningClose)
                                Require(release.Wait(MicrostateSourceTailCaseNames.Deadline));
                            if (mode == MicrostateSourceTailCaseNames.FirstFailure) throw sentinel;
                        }
                        request.Normal(lane);
                    });
                    request.BindStatisticsTrap(batch, trap);
                    RuntimeMicrostateSourceTail1149 tail = dispatcher.PrepareMicrostateSourceTail1149(
                        context,
                        batch,
                        request,
                        () =>
                        {
                            Interlocked.Increment(ref finishCalls);
                            Require(RuntimeNormalAuxiliaryTurn536.IsRuntime536(actualRuntime)
                                && RuntimeNormalAuxiliaryTurn536.Lane536 == sourceLane
                                && ReferenceEquals(RuntimeMicrostateThreadIdentity.Current, sourceThread));
                            if (mode == MicrostateSourceTailCaseNames.SuccessfulPhases)
                                request.FinishSourcePhase1171();
                            else throw sentinel;
                        }
                    );
                    int? published = dispatcher.PublishMicrostatesWithSourceTail1149(context, tail);
                    Require(published == sourceLane);
                    var actual = new MicrostateSourceTailPrepared(
                        context,
                        dispatcher,
                        request,
                        batch,
                        tail,
                        sourceLane,
                        sourceThread
                    );
                    ready.TrySetResult(actual);
                    try
                    {
                        await tail.Completion;
                        if (mode != MicrostateSourceTailCaseNames.SuccessfulPhases)
                            throw new InvalidOperationException(MicrostateSourceTailCaseNames.Contract);
                        Term result = await trap.RunHelper(
                            source,
                            StatisticsTrapNames.GatherMicrostates,
                            request.Completion,
                            [trap.Reference, Term.I(request.ReplyCount)]
                        );
                        expectedRows = request.ReplyCount;
                        helperRows = Cons.Items(result).Count();
                        helperEtf = ExternalTermFormat.Encode(result);
                        Require(helperRows == expectedRows);
                    }
                    catch (Exception error)
                    {
                        actorObserved = error;
                        if (!trap.Retirement.IsCompleted)
                        {
                            try
                            {
                                await trap.FailAndDrain(error).ConfigureAwait(false);
                            }
                            catch (Exception drained)
                            {
                                actorObserved = drained;
                            }
                        }
                    }
                }
                catch (Exception error)
                {
                    actorObserved = error;
                    ready.TrySetException(error);
                }

                return Nil.Value;
            });
            prepared = await ready.Task.WaitAsync(MicrostateSourceTailCaseNames.Deadline).ConfigureAwait(false);
            Require(RegistryCount(prepared.Dispatcher) == 1 && ownCalls == 0 && finishCalls == 0);
            await Task.WhenAll(prepared.Batch.Work.Where((_, index) => index != prepared.SourceLane - ProcessSchedulerNames.First)
                .Select(work => work.Completion)).WaitAsync(MicrostateSourceTailCaseNames.Deadline).ConfigureAwait(false);
            Require(!prepared.Batch.Work[prepared.SourceLane - ProcessSchedulerNames.First].Completion.IsCompleted);
            if (mode == MicrostateSourceTailCaseNames.SuccessfulPhases)
                prepared.Request.ApplyLocalUnmanaged1171();
            if (mode != MicrostateSourceTailCaseNames.WithheldClose)
            {
                prepared.Tail.QueueFirst();
                Require(entered.Wait(MicrostateSourceTailCaseNames.Deadline));
            }
            if (mode is MicrostateSourceTailCaseNames.FinishFailure or MicrostateSourceTailCaseNames.SuccessfulPhases)
            {
                await prepared.Tail.FirstCompletion.WaitAsync(MicrostateSourceTailCaseNames.Deadline).ConfigureAwait(false);
                Require(ownCalls == 1 && finishCalls == 0 && actualPhysicalSource);
                prepared.Tail.QueueSecond();
            }
            if (mode is MicrostateSourceTailCaseNames.WithheldClose or MicrostateSourceTailCaseNames.RunningClose)
            {
                closing = runtime.DisposeAsync().AsTask();
                if (mode == MicrostateSourceTailCaseNames.RunningClose)
                {
                    await WaitUntil(() => prepared.Context.Cancellation.IsCancellationRequested).ConfigureAwait(false);
                    Require(!prepared.Tail.Completion.IsCompleted && !prepared.Tail.FirstCompletion.IsCompleted
                        && !prepared.Request.Completion.IsCompleted && !closing.IsCompleted && ownCalls == 1);
                    release.Set();
                }
            }
            try
            {
                await prepared.Tail.Completion.WaitAsync(MicrostateSourceTailCaseNames.Deadline).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                Exception expected = mode is MicrostateSourceTailCaseNames.FirstFailure or MicrostateSourceTailCaseNames.FinishFailure
                    ? sentinel : prepared.Tail.Failure ?? throw new InvalidOperationException(MicrostateSourceTailCaseNames.Contract);
                if (mode is MicrostateSourceTailCaseNames.WithheldClose or MicrostateSourceTailCaseNames.RunningClose)
                {
                    await WaitUntil(() => prepared.Context.Cancellation.IsCancellationRequested).ConfigureAwait(false);
                    inspectedRequesterCancellation = prepared.Context.Cancellation;
                    Require(expected is OperationCanceledException canceled
                        && ReferenceEquals(expected, prepared.Tail.Failure)
                        && ReferenceEquals(expected, prepared.Tail.RetirementCancellation)
                        && canceled.CancellationToken == inspectedRequesterCancellation
                        && inspectedRequesterCancellation.IsCancellationRequested
                        && prepared.Tail.Completion.IsCanceled);
                    ownedShutdownCancellationInspected = true;
                }
                if (!Contains(error, expected))
                    throw new AggregateException(error, new InvalidOperationException(MicrostateSourceTailCaseNames.Contract));
                inspectedTailFailure = expected;
                tailFailureInspected = true;
            }
            Require(mode == MicrostateSourceTailCaseNames.SuccessfulPhases
                ? prepared.Tail.Completion.IsCompletedSuccessfully
                : gracefulShutdownMode
                    ? prepared.Tail.Completion.IsCanceled && ownedShutdownCancellationInspected
                    : prepared.Tail.Completion.IsFaulted);
            await actor.Completion.WaitAsync(MicrostateSourceTailCaseNames.Deadline).ConfigureAwait(false);
            closing ??= runtime.DisposeAsync().AsTask();
            await closing.WaitAsync(MicrostateSourceTailCaseNames.Deadline).ConfigureAwait(false);
            if (mode == MicrostateSourceTailCaseNames.SuccessfulPhases && actorObserved is not null)
                throw new AggregateException(actorObserved, new InvalidOperationException(MicrostateSourceTailCaseNames.Contract));
            Require(RegistryCount(prepared.Dispatcher) == 0 && prepared.Request.Completion.IsCompleted
                && prepared.Batch.Completion.IsCompleted
                && (mode == MicrostateSourceTailCaseNames.SuccessfulPhases ? actorObserved is null : actorObserved is not null)
                && ownCalls == (mode == MicrostateSourceTailCaseNames.WithheldClose ? 0 : 1)
                && finishCalls == (mode is MicrostateSourceTailCaseNames.FinishFailure or MicrostateSourceTailCaseNames.SuccessfulPhases ? 1 : 0));
            if (mode == MicrostateSourceTailCaseNames.SuccessfulPhases)
                Require(helperRows == expectedRows && helperEtf is { Length: > 0 });
            ProcessTermHeapSnapshot506 heap = prepared.Context.TermHeap506.Snapshot;
            Require(heap.RootFrames == 0 && heap.Blocks == 0 && heap.LivePayloadWords == 0);
            Require((int)typeof(ModuleRegistry).GetField(MicrostateSourceTailCaseNames.TrapUses, Members)!
                .GetValue(runtime.Modules)! == 0);
        }
        catch (Exception error)
        {
            primary = error;
        }
        finally
        {
            try
            {
                release.Set();
            }
            catch (Exception error)
            {
                errors[errorCount++] = error;
            }
            try
            {
                prepared?.Tail.Reject(sentinel);
            }
            catch (Exception error)
            {
                errors[errorCount++] = error;
            }
            try
            {
                if (prepared is not null) await prepared.Tail.Completion.WaitAsync(MicrostateSourceTailCaseNames.Deadline).ConfigureAwait(false);
            }
            catch (Exception error) when (tailFailureInspected && prepared is not null
                && (prepared.Tail.Completion.IsFaulted
                    || gracefulShutdownMode && ownedShutdownCancellationInspected
                        && prepared.Tail.Completion.IsCanceled
                        && inspectedRequesterCancellation.IsCancellationRequested
                        && inspectedTailFailure is OperationCanceledException inspectedCancellation
                        && inspectedCancellation.CancellationToken == inspectedRequesterCancellation
                        && ReferenceEquals(prepared.Tail.Failure, inspectedTailFailure)
                        && ReferenceEquals(inspectedTailFailure, prepared.Tail.RetirementCancellation)
                        && Contains(error, inspectedTailFailure)))
            {
                _ = error;
            }
            catch (Exception error)
            {
                errors[errorCount++] = error;
            }
            try
            {
                if (runtime is not null) await runtime.DisposeAsync();
            }
            catch (Exception error)
            {
                errors[errorCount++] = error;
            }
            try
            {
                if (actor is not null) await actor.Completion.WaitAsync(MicrostateSourceTailCaseNames.Deadline).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                errors[errorCount++] = error;
            }
        }
        if (errorCount != 0)
        {
            var failures = new List<Exception>();
            if (primary is not null) failures.Add(primary);
            for (int i = 0; i < errorCount; i++) failures.Add(errors[i]!);
            throw new AggregateException(failures);
        }
        if (primary is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary).Throw();

        return new
        {
            Mode = mode,
            OwnCalls = ownCalls,
            FinishCalls = finishCalls,
            ActualPhysicalSource = actualPhysicalSource,
            RegistryEmpty = prepared is not null && RegistryCount(prepared.Dispatcher) == 0,
            HelperRows = helperRows,
            ExpectedRows = expectedRows,
            HelperEtfBase64 = helperEtf is null ? null : Convert.ToBase64String(helperEtf)
        };
    }
}
