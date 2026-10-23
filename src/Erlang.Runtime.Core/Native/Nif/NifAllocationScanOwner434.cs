namespace Erlang;

internal sealed record NifAllocationScanOwner434(long Membership, WeakReference<NifNativeAllocationOwner> Owner, string Module);
