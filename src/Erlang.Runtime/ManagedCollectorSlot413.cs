namespace Erlang;

internal sealed class ManagedCollectorSlot413
{
    internal object? Reservation;
    internal ProcessContext? Actor;
    internal PreparedBeamImage? Source;

    internal void Clear()
    {
        Reservation = null;
        Actor = null;
        Source = null;
    }
}
