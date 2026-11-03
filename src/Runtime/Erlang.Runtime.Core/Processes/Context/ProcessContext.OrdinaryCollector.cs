namespace Erlang;

public sealed partial class ProcessContext
{
    private volatile bool ordinaryGuestCollector;

    // Only exact selected canonical Source/BEAM entry paths may call this.
    // Public profile options and opaque Host callback authority are unchanged.
    internal void AdmitOrdinaryGuestCollector()
    {
        if (OwnedExecutionEnabled683)
            return;
        if (!RuntimeActorExecutionTurn.IsOwnedBy(this) || CurrentOwned506 is not null
            || Volatile.Read(ref Terminated) || Runtime.IsHaltRequested)
            throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
        Cancellation.ThrowIfCancellationRequested();
        ordinaryGuestCollector = true;
        // Existing lifecycle admission serializes heap creation with VM drain.
        // If this loses or migration fails, actual context teardown owns cleanup.
        Runtime.RegisterGenuineVmContext683(this);
        Dictionary.ActivateGenuineVmExecution683();
    }
}
