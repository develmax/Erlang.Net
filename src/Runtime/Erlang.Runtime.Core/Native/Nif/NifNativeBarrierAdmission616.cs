namespace Erlang;

internal sealed class NifNativeBarrierAdmission616
{
    private readonly object gate = new();
    private readonly RuntimeActorDispatcher dispatcher;
    private readonly RuntimeNormalBarrierRequest696 request;
    private readonly ProcessContext actor;
    private readonly CancellationTokenRegistration cancellation;
    private readonly OperationCanceledException cancellationFailure;
    private bool claimed;
    private bool abandoned;
    internal NifActorAdmission616<RuntimeNormalBarrierRequest696> Awaiter616 { get; }

    internal NifNativeBarrierAdmission616(
        RuntimeActorDispatcher dispatcher,
        RuntimeNormalBarrierRequest696 request,
        ProcessContext actor
    )
    {
        this.dispatcher = dispatcher;
        this.request = request;
        this.actor = actor;
        Awaiter616 = new(
            request.Ready696.Task,
            actor,
            Claim616,
            Abandon616
        );
        cancellationFailure = new OperationCanceledException(actor.Cancellation);
        cancellation = actor.Cancellation.UnsafeRegister(static state =>
        {
            var admission = (NifNativeBarrierAdmission616)state!;
            admission.Cancel616();
        }, this);
    }

    private void Cancel616()
    {
        lock (gate)
            if (claimed) return;
        Awaiter616.RejectExternal616(cancellationFailure);
    }

    private RuntimeNormalBarrierRequest696 Claim616()
    {
        lock (gate)
        {
            actor.Cancellation.ThrowIfCancellationRequested();
            if (abandoned || claimed)
                throw new InvalidOperationException(RuntimeNormalBarrierNames696.Lease);
            claimed = true;
        }
        try
        {
            cancellation.Dispose();
        }
        catch (Exception error)
        {
            // The caller has not received this grant or entered native code yet.
            lock (gate) claimed = false;
            Abandon616(error);
            throw;
        }

        return request;
    }

    internal void Abandon616(Exception failure)
    {
        try
        {
            lock (gate)
            {
                if (claimed || abandoned) return;
                abandoned = true;
                dispatcher.AbandonNifBarrier616(request, failure);
            }
        }
        finally
        {
            // Outside ticket/dispatcher locks, including on the callback thread.
            cancellation.Dispose();
        }
    }
}
