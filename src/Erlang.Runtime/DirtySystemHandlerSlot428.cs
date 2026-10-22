namespace Erlang;

internal sealed class DirtySystemHandlerSlot428(ProcessSchedulingPriority priority)
{
    internal ProcessSchedulingPriority Priority { get; } = priority;
    internal ProcessContext? Actor { get; set; }
    internal PreparedBeamImage? Source { get; set; }
    internal object? Reservation { get; set; }

    internal void Clear()
    {
        Actor = null;
        Source = null;
        Reservation = null;
    }
}
