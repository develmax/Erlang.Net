namespace Erlang;

// Canonical identity/data adapter only. It owns NO host/guest root and has NO
// finalizer. Actual registered process headers and host wrappers own leases.
internal sealed class CountersGuestRoot421(RuntimeCountersOwner421 owner, CountersArray421 array)
{
    internal RuntimeCountersOwner421 Owner { get; } = owner;
    internal CountersArray421 Array { get; } = array;
    internal ProcessRuntime Runtime596 => Array.Runtime596;

    internal CountersBackingLease596 RetainGuest596() => Array.RetainBacking596(CountersLeaseKind596.Guest);

    internal CountersHostCustody596 RetainHost596() => new(this);

    internal nint MagicBinary596(CountersBackingLease596 lease) => Array.ReadMagicBinary596(lease);

    internal IReadOnlyList<uint> MagicNumbers596(CountersBackingLease596 lease) => Array.ReadMagicNumbers596(lease);
}
