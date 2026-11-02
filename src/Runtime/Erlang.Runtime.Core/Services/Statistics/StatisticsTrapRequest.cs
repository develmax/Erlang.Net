using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed class StatisticsTrapRequest
{
    private readonly ProcessRuntime runtime;
    private readonly ProcessContext caller;
    private ReferenceTerm? reference;
    internal ReferenceTerm Reference => reference ?? throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
    internal StatisticsTrapReceiveFailure ReceiveFailure { get; } = new();
    internal Task PhysicalCompletion { get; private set; } = Task.CompletedTask;

    internal StatisticsTrapRequest(ProcessRuntime runtime, ProcessContext caller, ReferenceTerm? reference = null)
    {
        this.runtime = runtime;
        this.caller = caller;
        this.reference = reference;
    }

    internal void PrepareReference() => reference ??= runtime.MakeReference();

    internal void Observe(RuntimeSchedulerAuxiliaryBatch420 batch)
    {
        if (!runtime.OwnsActorDispatcher428(batch.Owner))
            throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
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

    internal async ValueTask<Term> FailAndDrain(Exception primary)
        => await Finish(null, PhysicalCompletion, primary);

    private async ValueTask<Term> Finish(Func<ValueTask<Term>>? helper, Task physicalCompletion, Exception? primary)
    {
        Term? result = null;
        var failures = new StatisticsTrapFailures();
        if (primary is not null)
            failures.Add(primary);
        if (helper is not null)
            try
            {
                using var receive = caller.Mailbox.EnterStatisticsTrapReceive(ReceiveFailure);
                result = await helper();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        try
        {
            await physicalCompletion.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            // Task.Exception is the infrastructure WhenAll/TCS wrapper. Preserve
            // every actual supplied error, without flattening a user's aggregate.
            if (physicalCompletion.Exception is { } actual)
            {
                foreach (Exception failure in actual.InnerExceptions)
                    failures.Add(failure);
            }
            else
                failures.Add(error);
        }
        try
        {
            if (reference is { } actual)
                caller.Mailbox.RemoveStatisticsTrapReplies(actual);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        failures.ThrowIfAny();

        return result ?? throw new InvalidDataException(StatisticsTrapNames.InvalidImage);
    }
}
