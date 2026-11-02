using Erlang;

namespace Erlang.Tests;

internal static class NifAdmission626Checks
{
    internal static Task GateActorExit() => WithRuntime(async runtime =>
    {
        foreach (bool queued in new[] { false, true })
        {
            IDisposable? host = null;
            int delivered = 0, nonHost = 0;
            try
            {
                if (queued)
                    host = await NifAdmission626Access.Acquire(CancellationToken.None, static () => { });
                object? holder = NifAdmission626Access.Owner();
                var captured = Signal<(ProcessContext Context, NifAdmission626Awaiter Admission)>();
                ProcessHandle actor = runtime.Spawn(context =>
                {
                    NifAdmission626Awaiter admission = NifAdmission626Access.Acquire(
                        CancellationToken.None,
                        () =>
                    {
                        Interlocked.Increment(ref delivered);
                        if (NifAdmission626Access.Actor is not null) Interlocked.Increment(ref nonHost);
                    }
                    );
                    Require(queued ? !NifAdmission626Access.Ready(admission).IsCompleted
                        : NifAdmission626Access.Ready(admission).IsCompletedSuccessfully);
                    captured.TrySetResult((context, admission));

                    return ValueTask.FromResult<Term>(Nil.Value);
                });
                var observation = await captured.Task.WaitAsync(NifAdmission626Names.Deadline);
                await actor.Completion.WaitAsync(NifAdmission626Names.Deadline);
                Require(observation.Context.Cancellation.IsCancellationRequested);
                await FailedHostDelivery(observation.Admission);
                Require(delivered == 1 && nonHost == 0 && NifAdmission626Access.Waiting == 0);
                Require(ReferenceEquals(NifAdmission626Access.Owner(), holder));
            }
            finally
            {
                host?.Dispose();
            }
            await ReusableGate();
        }
    });

    internal static Task ClaimedScope() => WithRuntime(async runtime =>
    {
        var scopePlan = NifAdmission626Access.PrepareScope();
        using var release = new ManualResetEventSlim();
        using var requestCancellation = new CancellationTokenSource();
        using var probeCancellation = new CancellationTokenSource();
        var entered = Signal<object>();
        var physicalDone = Signal<bool>();
        ProcessContext? actual = null;
        ProcessHandle target = runtime.Spawn(async context =>
        {
            actual = context;
            try
            {
                using IDisposable permit = await NifAdmission626Access.Acquire(requestCancellation.Token, static () => { });
                Require(ReferenceEquals(NifAdmission626Access.Actor, context));
                scopePlan.Enter(permit, scopePlan.Box);
                try
                {
                    entered.TrySetResult(permit);
                    Require(release.Wait(NifAdmission626Names.Deadline));
                    Require(ReferenceEquals(NifAdmission626Access.Owner(), permit));
                    Require(NifAdmission626Access.State(permit) == NifAdmission626Names.EnteredState);
                }
                finally
                {
                    ((IDisposable)scopePlan.Box).Dispose();
                }
                Require(NifAdmission626Access.State(permit) == NifAdmission626Names.ClaimedState);
            }
            catch (Exception error)
            {
                physicalDone.TrySetException(error);
                throw;
            }
            finally
            {
                physicalDone.TrySetResult(true);
            }

            return Nil.Value;
        });
        Task? probe = null;
        try
        {
            object holder = await entered.Task.WaitAsync(NifAdmission626Names.Deadline);
            probe = FailedHostDelivery(NifAdmission626Access.Acquire(probeCancellation.Token, static () => { }));
            Require(SpinWait.SpinUntil(() => NifAdmission626Access.Waiting > 0, NifAdmission626Names.Deadline));
            requestCancellation.Cancel();
            ProcessHandle killer = runtime.Spawn(context =>
            {
                runtime.Exit(context, target.Pid, Term.A(NifAdmission626Names.Kill));

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            await killer.Completion.WaitAsync(NifAdmission626Names.Deadline);
            Require(actual!.Cancellation.IsCancellationRequested);
            Require(ReferenceEquals(NifAdmission626Access.Owner(), holder));
            Require(NifAdmission626Access.State(holder) == NifAdmission626Names.EnteredState);
            Require(!probe.IsCompleted);
            probeCancellation.Cancel();
            await probe.WaitAsync(NifAdmission626Names.Deadline);
            Require(ReferenceEquals(NifAdmission626Access.Owner(), holder));
        }
        finally
        {
            probeCancellation.Cancel();
            release.Set();
            await Settle(physicalDone.Task, probe, target.Completion);
        }
        await ReusableGate();
    });

    internal static Task BarrierActorExit() => WithRuntime(async runtime =>
    {
        int delivered = 0, nonHost = 0;
        var captured = Signal<(ProcessContext Context, NifAdmission626Awaiter Admission)>();
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            using IDisposable permit = await NifAdmission626Access.Acquire(CancellationToken.None, static () => { });
            NifAdmission626Awaiter admission = NifAdmission626Access.Barrier(
                context,
                () =>
            {
                Interlocked.Increment(ref delivered);
                if (NifAdmission626Access.Actor is not null) Interlocked.Increment(ref nonHost);
            }
            );
            // Await only the real producer readiness, without claiming its grant.
            await NifAdmission626Access.Ready(admission);
            Require(ReferenceEquals(NifAdmission626Access.Actor, context));
            captured.TrySetResult((context, admission));

            return Nil.Value;
        });
        var observation = await captured.Task.WaitAsync(NifAdmission626Names.Deadline);
        await actor.Completion.WaitAsync(NifAdmission626Names.Deadline);
        Require(observation.Context.Cancellation.IsCancellationRequested);
        await FailedHostDelivery(observation.Admission);
        Require(delivered == 1 && nonHost == 0);
        await NormalProgress(runtime);
        await ReusableGate();
    });

    internal static Task PendingBarrierCancellation() => WithRuntime(async runtime =>
    {
        using var blockerRelease = new ManualResetEventSlim();
        var blockerEntered = Signal<bool>();
        var captured = Signal<(ProcessContext Context, NifAdmission626Awaiter Admission)>();
        var requesterSettled = Signal<bool>();
        int delivered = 0, nonHost = 0;
        ProcessHandle blocker = runtime.Spawn(context =>
        {
            blockerEntered.TrySetResult(true);
            Require(blockerRelease.Wait(NifAdmission626Names.Deadline));

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        ProcessHandle? requester = null;
        try
        {
            await blockerEntered.Task.WaitAsync(NifAdmission626Names.Deadline);
            requester = runtime.Spawn(async context =>
            {
                try
                {
                    using IDisposable permit = await NifAdmission626Access.Acquire(CancellationToken.None, static () => { });
                    NifAdmission626Awaiter? admission = null;
                    admission = NifAdmission626Access.Barrier(
                        context,
                        () =>
                    {
                        Interlocked.Increment(ref delivered);
                        if (NifAdmission626Access.Actor is not null) Interlocked.Increment(ref nonHost);
                    },
                        () => captured.TrySetResult((context, admission!))
                    );
                    Require(!NifAdmission626Access.Ready(admission).IsCompleted);
                    try
                    {
                        using IDisposable impossible = await admission;
                        throw new InvalidOperationException(NifAdmission626Names.Broken);
                    }
                    catch (OperationCanceledException) { }
                }
                catch (Exception error)
                {
                    requesterSettled.TrySetException(error);
                    throw;
                }
                finally
                {
                    requesterSettled.TrySetResult(true);
                }

                return Nil.Value;
            });
            var observation = await captured.Task.WaitAsync(NifAdmission626Names.Deadline);
            DirtyNative419Access.Cancel(observation.Context);
            await requesterSettled.Task.WaitAsync(NifAdmission626Names.Deadline);
            int observedDelivered = Volatile.Read(ref delivered);
            int observedNonHost = Volatile.Read(ref nonHost);
            if (observedDelivered != 1 || observedNonHost != 0)
                throw new InvalidOperationException(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    NifAdmission626Names.PendingFailure,
                    observedDelivered,
                    observedNonHost,
                    observation.Context.Cancellation.IsCancellationRequested,
                    NifAdmission626Access.Ready(observation.Admission).IsCompleted
                ));
        }
        finally
        {
            blockerRelease.Set();
            await Settle(blocker.Completion, requester?.Completion);
        }
        await NormalProgress(runtime);
        await ReusableGate();
    });

    private static async Task FailedHostDelivery(NifAdmission626Awaiter admission)
    {
        bool failed = false;
        try
        {
            using IDisposable unexpected = await admission;
        }
        catch (OperationCanceledException)
        {
            failed = true;
        }
        Require(failed);
    }

    private static async Task ReusableGate()
    {
        using IDisposable permit = await NifAdmission626Access.Acquire(CancellationToken.None, static () => { });
        Require(ReferenceEquals(NifAdmission626Access.Owner(), permit));
        Require(NifAdmission626Access.Waiting == 0);
    }

    private static async Task NormalProgress(ProcessRuntime runtime)
    {
        var progressed = Signal<bool>();
        ProcessHandle actor = runtime.Spawn(context =>
        {
            progressed.TrySetResult(true);

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        await progressed.Task.WaitAsync(NifAdmission626Names.Deadline);
        await actor.Completion.WaitAsync(NifAdmission626Names.Deadline);
    }

    private static async Task WithRuntime(Func<ProcessRuntime, Task> body)
    {
        var runtime = new ProcessRuntime(2);
        Exception? primary = null, cleanup = null;
        try
        {
            await runtime.InitializeModulesAsync(_ => { });
            await body(runtime);
        }
        catch (Exception error)
        {
            primary = error;
        }
        try
        {
            await runtime.DisposeAsync().AsTask().WaitAsync(NifAdmission626Names.Deadline);
        }
        catch (Exception error)
        {
            cleanup = error;
        }
        if (primary is not null && cleanup is not null) throw new AggregateException(primary, cleanup);
        if (primary is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary).Throw();
        if (cleanup is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanup).Throw();
    }

    private static async Task Settle(params Task?[] tasks)
    {
        var errors = new List<Exception>();
        foreach (Task? task in tasks)
            if (task is not null)
                try
                {
                    await task.WaitAsync(NifAdmission626Names.Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
        if (errors.Count != 0) throw new AggregateException(errors);
    }

    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(NifAdmission626Names.Broken);
    }
}
