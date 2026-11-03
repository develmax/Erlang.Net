namespace Erlang;

internal sealed class StatisticsTrapRequest : IAsyncDisposable
{
    private readonly ProcessRuntime runtime;
    private readonly ProcessContext caller;
    private ReferenceTerm? reference;
    private readonly TaskCompletionSource retirement = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private RuntimeSchedulerAuxiliaryBatch420? observedBatch;
    private Action<Exception>? abortPreparation;
    internal Task Retirement => retirement.Task;

    internal void CompleteRetirement() => retirement.TrySetResult();

    internal void BindPreparationAbort(Action<Exception> abort) => abortPreparation = abort;

    internal ReferenceTerm Reference => reference ?? throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
    internal StatisticsTrapReceiveFailure ReceiveFailure { get; } = new();
    internal Task PhysicalCompletion { get; private set; } = Task.CompletedTask;

    internal StatisticsTrapRequest(ProcessRuntime runtime, ProcessContext caller, ReferenceTerm? reference = null)
    {
        this.runtime = runtime;
        this.caller = caller;
        this.reference = reference;
        runtime.RegisterStatisticsTrap(caller, this);
    }

    internal void PrepareReference() => reference ??= runtime.MakeReference();

    internal void Observe(RuntimeSchedulerAuxiliaryBatch420 batch)
    {
        if (!runtime.OwnsActorDispatcher428(batch.Owner))
            throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
        observedBatch = batch;
        PhysicalCompletion = batch.Completion;
        foreach (RuntimeSchedulerAuxiliary420 work in batch.Work)
            ReceiveFailure.Observe(work.Completion);
    }

    internal void BindPhysical(Task completion)
    {
        PhysicalCompletion = completion;
        ReceiveFailure.Observe(completion);
    }

    internal void Reply(Term? payload = null)
        => runtime.SendSystem(caller.Self, payload is null ? Reference : Term.Tuple(Reference, payload), Term.A(StatisticsTrapNames.System));

    internal async ValueTask<Term> RunHelper(
        StatisticsTrapAdmission admission,
        string function,
        Task physicalCompletion,
        params Term[] arguments
    )
    {
        if (!ReferenceEquals(physicalCompletion, PhysicalCompletion))
            return await FailAndDrain(new InvalidOperationException(StatisticsTrapNames.InvalidImage));

        return await Finish(() => runtime.Modules.InvokeStatisticsTrap(
            caller,
            admission,
            function,
            arguments
        ), physicalCompletion, null);
    }

    internal async ValueTask<Term> RunGcHelper(GcStatisticsAdmission admission, Task physicalCompletion)
    {
        if (!ReferenceEquals(physicalCompletion, PhysicalCompletion))
            return await FailAndDrain(new InvalidOperationException(GcStatisticsNames.Invalid));

        return await Finish(() => runtime.Modules.InvokeGcStatistics(caller, admission, Reference), physicalCompletion, null);
    }

    internal async ValueTask<Term> FailAndDrain(Exception primary)
        => await Finish(null, PhysicalCompletion, primary);

    public async ValueTask DisposeAsync()
    {
        if (Retirement.IsCompleted) return;
        Exception? unpublished = null, batchFailure = null, preparationFailure = null, drainFailure = null;
        if (observedBatch is { State: RuntimeSchedulerAuxiliaryState420.Prepared } batch)
        {
            // No callback or scheduler ACK exists for an unpublished no-op.
            // Abort only provisional work; published callbacks always drain naturally.
            unpublished = new OperationCanceledException();
            try
            {
                batch.Abort(unpublished);
            }
            catch (Exception error)
            {
                batchFailure = error;
            }
            try
            {
                abortPreparation?.Invoke(unpublished);
            }
            catch (Exception error)
            {
                preparationFailure = error;
            }
        }
        try
        {
            _ = await Finish(null, PhysicalCompletion, null);
        }
        catch (Exception error)
        {
            drainFailure = error;
        }
        // All mandatory work and reply cleanup have settled before aggregation.
        if (batchFailure is null && preparationFailure is null
            && (drainFailure is null || ReferenceEquals(drainFailure, unpublished)))
            return;

        var failures = new StatisticsTrapFailures();
        if (batchFailure is not null) failures.Add(batchFailure);
        if (preparationFailure is not null) failures.Add(preparationFailure);
        if (drainFailure is not null && !ReferenceEquals(drainFailure, unpublished)) failures.Add(drainFailure);
        failures.ThrowIfAny();
    }

    private async ValueTask<Term> Finish(Func<ValueTask<Term>>? helper, Task physicalCompletion, Exception? primary)
    {
        Term? result = null;
        Exception? helperFailure = null, physicalFailure = null, cleanupFailure = null;
        try
        {
            try
            {
                if (helper is not null)
                    try
                    {
                        using var receive = caller.Mailbox.EnterStatisticsTrapReceive(ReceiveFailure);
                        result = await helper();
                    }
                    catch (Exception error)
                    {
                        helperFailure = error;
                    }
            }
            finally
            {
                try
                {
                    await physicalCompletion.ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    physicalFailure = error;
                }
                finally
                {
                    try
                    {
                        if (reference is { } actual)
                            caller.Mailbox.RemoveStatisticsTrapReplies(actual);
                    }
                    catch (Exception error)
                    {
                        cleanupFailure = error;
                    }
                }
            }
            // Allocate error bookkeeping only after the real physical join and
            // reply cleanup. Bookkeeping failure cannot certify early retirement.
            var failures = new StatisticsTrapFailures();
            if (primary is not null) failures.Add(primary);
            if (helperFailure is not null) failures.Add(helperFailure);
            if (physicalFailure is not null)
            {
                if (physicalCompletion.Exception is { } actual)
                {
                    // Unwrap only the infrastructure task wrapper, never a
                    // user-supplied aggregate exception.
                    foreach (Exception failure in actual.InnerExceptions) failures.Add(failure);
                }
                else
                    failures.Add(physicalFailure);
            }
            if (cleanupFailure is not null) failures.Add(cleanupFailure);
            failures.ThrowIfAny();

            return result ?? (helper is null ? Nil.Value : throw new InvalidDataException(StatisticsTrapNames.InvalidImage));
        }
        finally
        {
            runtime.SettleStatisticsTrap(caller, this);
        }
    }
}
