namespace Erlang;

// Canonical identity/data adapter only. It owns NO host/guest root and has NO
// finalizer. Actual registered process headers and host wrappers own leases.
internal sealed class AtomicsGuestRoot413(RuntimeAtomicsOwner413 owner, AtomicsArray413 array)
{
    internal RuntimeAtomicsOwner413 Owner { get; } = owner;
    internal AtomicsArray413 Array { get; } = array;
    internal ProcessRuntime Runtime586 => Array.Runtime586;

    internal AtomicsBackingLease586 RetainGuest586() => Array.RetainBacking586(AtomicsLeaseKind586.Guest);

    internal AtomicsHostCustody586 RetainHost586() => new(this);

    internal nint MagicBinary586(AtomicsBackingLease586 lease) => Array.ReadMagicBinary586(lease);

    internal IReadOnlyList<uint> MagicNumbers586(AtomicsBackingLease586 lease) => Array.ReadMagicNumbers586(lease);
}
