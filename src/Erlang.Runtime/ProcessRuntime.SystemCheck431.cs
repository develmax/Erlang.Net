namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> SystemCheckSchedulers431(ProcessContext caller)
    {
        // The native C trap executes this unchanged preloaded Erlang helper too.
        // Do not substitute an immediate ok or translate its receive loop to C#.
        if (Modules.SourceOwner(InternalSystemCheckNames431.Module, InternalSystemCheckNames431.Gather, 1) is not PreparedBeamImage image
            || !ReferenceEquals(image.Owner, Modules)) throw new NotSupportedException(InternalSystemCheckNames431.SourceHelper);
        RuntimeSchedulerAuxiliaryBatch420 batch;
        ReferenceTerm reference;
        Exception? publicationFailure = null;
        lock (gate)
        {
            RequireEtsFoundationActor(caller);
            caller.Cancellation.ThrowIfCancellationRequested();
            reference = MakeReference();
            Pid requester = caller.Self;
            batch = actorDispatcher.PrepareAuxiliary420(_ => SendSystem(requester, reference, Term.A(InternalSystemCheckNames431.System)));
            try
            {
                batch.Publish();
            }
            catch (Exception error)
            {
                publicationFailure = error;
            }
        }
        if (publicationFailure is not null)
        {
            // Publish aborts its not-yet-executable nodes under the dispatcher gate.
            // Observe that aggregate outside runtime.gate; preserve both failures.
            try
            {
                await batch.Completion.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                throw new AggregateException(publicationFailure, error);
            }
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(publicationFailure).Throw();
        }
        Exception? waitFailure = null;
        try
        {
            await caller.Mailbox.WaitForControlCompletion(batch.Completion, caller.Cancellation);
        }
        catch (Exception error)
        {
            waitFailure = error;
        }
        try
        {
            // A caller exit cannot abandon actual queued acknowledgements.
            await batch.Completion.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            Exception failure = waitFailure is null || ReferenceEquals(waitFailure, error) ? error : new AggregateException(waitFailure, error);
            RemoveSystemCheckReferences431(caller, reference, failure);
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        if (waitFailure is not null)
        {
            RemoveSystemCheckReferences431(caller, reference, waitFailure);
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(waitFailure).Throw();
        }
        try
        {
            caller.Cancellation.ThrowIfCancellationRequested();

            // Every real auxiliary callback has finished. The unchanged source
            // helper consumes all N actual messages before the native returns.
            return await Modules.Call(
                caller,
                InternalSystemCheckNames431.Module,
                InternalSystemCheckNames431.Gather,
                reference
            );
        }
        catch (Exception error)
        {
            RemoveSystemCheckReferences431(caller, reference, error);
            throw;
        }
    }

    private void RemoveSystemCheckReferences431(ProcessContext caller, ReferenceTerm reference, Exception failure)
    {
        // All callbacks have physically settled; no later ACK can arrive.
        // This protocol-only removal must not readmit a pending kill/control.
        try
        {
            caller.Mailbox.RemoveSystemCheckReferences431(reference);
        }
        catch (Exception cleanup)
        {
            throw new AggregateException(failure, cleanup);
        }
    }
}
