namespace Erlang;

internal sealed class CountersGuestRoot421(RuntimeCountersOwner421 owner, CountersArray421 array)
{
    internal RuntimeCountersOwner421 Owner { get; } = owner;
    internal CountersArray421 Array { get; } = array;

    ~CountersGuestRoot421()
    {
        // No guest Term retained by the allocation owner. Unreachable roots can retire independently of process death.
        try
        {
            Owner.Retire(Array);
        }
        catch { /* deterministic runtime Close remains the reporting/join path */ }
    }
}
