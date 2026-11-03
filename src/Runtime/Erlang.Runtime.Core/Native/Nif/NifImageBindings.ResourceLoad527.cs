namespace Erlang;

internal sealed partial class NifImageBindings
{
    private Exception?[]? resourcePublicationFailures527;

    internal void RecordResourcePublicationFailures527(Exception?[] failures)
        => Interlocked.CompareExchange(ref resourcePublicationFailures527, failures, null);

    internal NifResourceImagePin524 RetainLoadingResource527(NifImageBindingReservation authority)
    {
        var pin = new NifResourceImagePin524(this);
        lock (gate)
        {
            if (phase != NifImageBindingPhase.Initializing || !ReferenceEquals(reservation, authority))
                throw new InvalidOperationException(NativeResource527Names.LoadPhase);
            activeCalls = checked(activeCalls + 1);
            pin.Admit524();
        }

        return pin;
    }
}
