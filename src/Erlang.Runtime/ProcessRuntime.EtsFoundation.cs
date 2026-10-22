namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeEtsFoundation? etsFoundation;

    internal RuntimeEtsFoundation EtsFoundation
    {
        get
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);

                return etsFoundation ??= new(this, gate);
            }
        }
    }

    private Task DrainEtsRetirements()
    {
        lock (gate)
            return etsFoundation?.DrainAsync() ?? Task.CompletedTask;
    }

    private async Task CompleteHaltEtsRetirements(ProcessContext[] stopped)
    {
        Exception? failure = null;
        Task retirements = Task.WhenAll(
            DrainEtsRetirements(),
            DrainPersistentTerms(),
            DrainRegex411(),
            DrainDirtyNative419(),
            DrainDebugDispatch432(),
            DrainCounters421(),
            DrainHamtIndex429(),
            DrainDriverFrontend422(),
            DrainCodePurger432(),
            DrainAllocatorGather434()
        );
        try
        {
            await retirements.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failure = retirements.Exception ?? error;
        }
        lock (gate)
        {
            foreach (var process in stopped)
            {
                if (failure is null)
                    process.Done.TrySetResult(process.ExitReason!);
                else
                    process.Done.TrySetException(failure);
            }
        }
        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    // The caller holds the actual shared runtime gate, not a second table gate.
    internal void RequireEtsFoundationActor(ProcessContext context)
    {
        if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(RuntimeEtsFoundationNames.ForeignActor);
        ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);
        if (context.Terminated || !processes.TryGetValue(context.Self, out var active) || !ReferenceEquals(active, context))
            throw new InvalidOperationException(RuntimeEtsFoundationNames.DeadActor);
    }
}
