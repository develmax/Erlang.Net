namespace Erlang;

// This record owns the real backing lease of ONE actual registered three-word
// wrapper. It is not an alternative identity or a counted shadow allocation.
internal sealed class NativeOffheapHeader576(
    ProcessTermHeap506 owner,
    ProcessTermSequence506 block,
    NativeResourcePayload524 payload,
    nint binary,
    nint address,
    NativeOffheapKind576 kind,
    NativeResourceRootLease524 lease
)
{
    private NativeResourceRootLease524? retained = lease;
    internal ProcessTermHeap506 Owner { get; } = owner;
    internal ProcessTermSequence506 Block { get; } = block;
    internal NativeResourcePayload524 Payload { get; } = payload;
    internal nint Binary { get; } = binary;
    internal nint Address { get; } = address;
    internal NativeOffheapKind576 Kind { get; } = kind;
    internal ResourceReferenceTerm? MagicTerm { get; set; }
    internal bool Published { get; set; }

    internal void Release576(bool guestGc)
    {
        NativeResourceRootLease524? selected = Interlocked.Exchange(ref retained, null);
        if (guestGc)
            selected?.DisposeGuestGc536();
        else
            selected?.Dispose();
    }
}
