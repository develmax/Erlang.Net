namespace Erlang;

internal sealed class AtomicsGuestRoot413(RuntimeAtomicsOwner413 owner, AtomicsArray413 array)
{
    internal RuntimeAtomicsOwner413 Owner { get; } = owner;
    internal AtomicsArray413 Array { get; } = array;

    ~AtomicsGuestRoot413()
    {
        // No guest Term retained by the allocation owner. Unreachable roots can retire independently of process death.
        try
        {
            Owner.Retire(Array);
        }
        catch { /* deterministic runtime Close remains the reporting/join path */ }
    }
}
