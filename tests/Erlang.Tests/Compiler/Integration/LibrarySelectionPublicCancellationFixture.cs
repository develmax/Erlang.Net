using static Erlang.Tests.LibrarySelectionHostData;

namespace Erlang.Tests;

internal sealed class LibrarySelectionPublicCancellationFixture(string heldPhase)
{
    internal readonly CancellationTokenSource Cancellation = new();
    internal readonly ManualResetEventSlim Release = new(false);
    internal readonly TaskCompletionSource Reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource CleanupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource RetirementMarked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal ProcessRuntime Runtime = null!;
    internal ProcessContext? Worker;
    internal ModuleCodeImage[] Bootstrap = [];
    internal bool InitialOwnersWereManaged;
    internal Task<ProcessRuntime> Factory = null!;

    internal void Start()
    {
        AsyncLocal<Action<string, ProcessRuntime, ProcessContext?>?> observer = LibrarySelectionPublicCancellationAccess.Observer();
        Action<string, ProcessRuntime, ProcessContext?>? previous = observer.Value;
        observer.Value = Observe;
        try
        {
            // Task.Run captures this ExecutionContext. Restore only the caller's
            // context immediately; the factory must retain its own captured observer.
            Factory = Task.Run(() => ProcessRuntime.CreateAsync(Both(), Cancellation.Token));
        }
        finally
        {
            observer.Value = previous;
        }
    }

    private void Observe(string phase, ProcessRuntime runtime, ProcessContext? worker)
    {
        Runtime = runtime;
        if (phase == LibrarySelectionPublicCancellationNames.SetupPhase)
        {
            Bootstrap = [runtime.Modules.CurrentCodeImage(ListBeamHostNames.Module)!, runtime.Modules.CurrentCodeImage(QueueModuleNames.Module)!];
            InitialOwnersWereManaged = runtime.Modules.SourceOwner(ListBeamHostNames.Module, ListBeamHostNames.Sort, 1) is null
                && runtime.Modules.SourceOwner(QueueModuleNames.Module, QueueModuleNames.New, 0) is null
                && !runtime.Modules.HasOldCode(ListBeamHostNames.Module) && !runtime.Modules.HasOldCode(QueueModuleNames.Module);
        }
        if (phase == LibrarySelectionPublicCancellationNames.CleanupStartedPhase)
            CleanupStarted.TrySetResult();
        if (phase == LibrarySelectionPublicCancellationNames.RetirementMarkedPhase)
            RetirementMarked.TrySetResult();
        if (phase != heldPhase)
            return;
        Worker = worker;
        Reached.TrySetResult();
        if (!Release.Wait(LibrarySelectionPublicCancellationNames.Deadline))
            throw new TimeoutException(LibrarySelectionPublicCancellationNames.Failure);
    }

    internal async Task ExpectCancellation()
    {
        bool canceled = false;
        try
        {
            _ = await Factory.WaitAsync(LibrarySelectionPublicCancellationNames.Deadline);
        }
        catch (OperationCanceledException error) when (error.CancellationToken == Cancellation.Token)
        {
            canceled = true;
        }
        Require(canceled && Factory.IsCanceled);
    }

    internal async Task Cleanup(List<Exception> failures)
    {
        bool physicallySettled = false;
        Release.Set();
        Cancellation.Cancel();
        try
        {
            if (Factory is not null)
            {
                ProcessRuntime? returned = null;
                try
                {
                    returned = await Factory.WaitAsync(LibrarySelectionPublicCancellationNames.Deadline);
                }
                catch (OperationCanceledException error) when (error.CancellationToken == Cancellation.Token)
                {
                }
                if (returned is not null)
                    await returned.DisposeAsync().AsTask().WaitAsync(LibrarySelectionPublicCancellationNames.Deadline);
                if (Runtime is not null && LibrarySelectionPublicCancellationAccess.Disposed(Runtime))
                {
                    await LibrarySelectionPublicCancellationAccess.Dispatcher(Runtime).WaitAsync(LibrarySelectionPublicCancellationNames.Deadline);
                    physicallySettled = true;
                }
            }
            if (Runtime is null && (Factory is null || Factory.IsCompleted))
                physicallySettled = true;
        }
        catch (Exception error)
        {
            if (!failures.Contains(error))
                failures.Add(error);
            if (Factory is not null)
                _ = Factory.ContinueWith(
                    task => _ = task.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default
                );
        }
        finally
        {
            // Only dispose the barrier after the owned factory/actor has settled.
            if (physicallySettled)
                Release.Dispose();
            Cancellation.Dispose();
        }
    }
}
