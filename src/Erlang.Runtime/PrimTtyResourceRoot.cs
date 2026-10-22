namespace Erlang;

internal sealed class PrimTtyResourceRoot(PrimTtyBuiltinOwner owner, PrimTtyResourceState state)
{
    internal PrimTtyBuiltinOwner Owner { get; } = owner;
    internal PrimTtyResourceState State { get; } = state;

    ~PrimTtyResourceRoot()
    {
        // State ownership is independent of this guest root. Retirement task
        // and faults are retained by the owner and joined on owner shutdown.
        try
        {
            Owner.Retire(State);
        }
        catch { /* A finalizer must not terminate the process. Owner Close joins state. */ }
    }
}
