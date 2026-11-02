using System.Runtime.ExceptionServices;
using static Erlang.Tests.LibrarySelectionCancellationPhaseAccess;
using static Erlang.Tests.LibrarySelectionHostData;

namespace Erlang.Tests;

internal static class LibrarySelectionCancellationPhaseHostChecks
{
    internal static async Task Run()
    {
        var runtime = new ProcessRuntime();
        using var cancellation = new CancellationTokenSource();
        var failures = new List<Exception>();
        SemaphoreSlim purger = Purger(runtime);
        bool held = false;
        Task? disposal = null;
        ProcessContext? worker = null;
        Action? previousSignals = null;
        try
        {
            ModuleCodeImage[] bootstrap = [runtime.Modules.CurrentCodeImage(ListBeamHostNames.Module)!, runtime.Modules.CurrentCodeImage(QueueModuleNames.Module)!];
            using var plan = (IDisposable)Prepare(runtime.Modules, Both());
            await runtime.InitializeModulesAsync(registry =>
            {
                Publish(registry, plan);
            }).AsTask().WaitAsync(LibrarySelectionCancellationPhaseNames.Deadline);
            Require(runtime.Modules.HasOldCode(ListBeamHostNames.Module) && runtime.Modules.HasOldCode(QueueModuleNames.Module));
            await purger.WaitAsync().WaitAsync(LibrarySelectionCancellationPhaseNames.Deadline);
            held = true;
            var reachedWait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<RuntimeLibrarySelectionSnapshot> finishing;
            // Hold only the existing process-table gate, with no await. Spawn registers
            // its context synchronously; the dispatched actor first takes this gate.
            lock (RuntimeGate(runtime))
            {
                HashSet<Pid> before = ProcessesWhileLocked(runtime).Select(context => context.Self).ToHashSet();
                finishing = Finish(runtime, plan!, cancellation.Token);
                worker = ProcessesWhileLocked(runtime).Single(context => !before.Contains(context.Self));
                previousSignals = (Action?)Signals.GetValue(worker.Mailbox);
                Action? original = previousSignals;
                Signals.SetValue(worker.Mailbox, (Action)(() =>
                {
                    original?.Invoke();
                    reachedWait.TrySetResult();
                }));
            }
            await reachedWait.Task.WaitAsync(LibrarySelectionCancellationPhaseNames.Deadline);
            Require(purger.CurrentCount == 0 && !finishing.IsCompleted);
            Require(!Done(worker).IsCompleted && !Unwound(worker).IsCompleted);
            Require(ActiveLeases(worker) == 0 && bootstrap.All(image => runtime.Modules.PendingPurge(image) is null));
            cancellation.Cancel();
            bool canceled = false;
            try
            {
                _ = await finishing.WaitAsync(LibrarySelectionCancellationPhaseNames.Deadline);
            }
            catch (OperationCanceledException error) when (error.CancellationToken == cancellation.Token)
            {
                canceled = true;
            }
            Require(canceled && finishing.IsCanceled);
            // Private phase cancellation only cancels its caller. The owning factory
            // disposes the runtime; exercise that cleanup explicitly with admission held.
            Require(!Done(worker).IsCompleted && !Unwound(worker).IsCompleted);
            disposal = runtime.DisposeAsync().AsTask();
            Term reason = await Done(worker).WaitAsync(LibrarySelectionCancellationPhaseNames.Deadline);
            await Unwound(worker).WaitAsync(LibrarySelectionCancellationPhaseNames.Deadline);
            await disposal.WaitAsync(LibrarySelectionCancellationPhaseNames.Deadline);
            Require(reason.Equals(Term.A(ProcessExitReasons.Shutdown)));
            Require(ActiveLeases(worker) == 0 && bootstrap.All(image => runtime.Modules.PendingPurge(image) is null));
            lock (RuntimeGate(runtime))
                Require(!ProcessesWhileLocked(runtime).Any(context => context.Self.Equals(worker.Self)));
            Require(purger.CurrentCount == 0);
            purger.Release();
            held = false;
            // A canceled waiter must not steal the test owner's single released permit.
            Require(purger.Wait(0));
            purger.Release();
            Require(purger.CurrentCount == 1);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            disposal ??= runtime.DisposeAsync().AsTask();
            try
            {
                await disposal.WaitAsync(LibrarySelectionCancellationPhaseNames.Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (held)
                purger.Release();
            if (worker is not null)
                Signals.SetValue(worker.Mailbox, previousSignals);
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(LibrarySelectionCancellationPhaseNames.Failure, failures);
    }
}
