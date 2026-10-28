namespace Erlang;

// This record owns the real backing lease of ONE actual registered three-word
// wrapper. It is not an alternative identity or a counted shadow allocation.
internal sealed class NativeOffheapHeader576
{
    private NativeResourceRootLease524? retained;
    private AtomicsBackingLease586? atomicsRetained586;
    private readonly NativeResourcePayload524? payload;
    internal AtomicsGuestRoot413? Atomics586 { get; }
    internal ProcessTermHeap506 Owner { get; }
    internal ProcessTermSequence506 Block { get; }
    internal NativeResourcePayload524 Payload => payload ?? throw new InvalidOperationException(MagicReference576Names.Invalid);
    internal nint Binary { get; }
    internal nint Address { get; }
    internal NativeOffheapKind576 Kind { get; }
    internal ResourceReferenceTerm? MagicTerm { get; set; }
    internal bool Published { get; set; }

    internal NativeOffheapHeader576(
        ProcessTermHeap506 owner,
        ProcessTermSequence506 block,
        NativeResourcePayload524 payload,
        nint binary,
        nint address,
        NativeOffheapKind576 kind,
        NativeResourceRootLease524 lease
    )
    {
        Owner = owner;
        Block = block;
        this.payload = payload;
        Binary = binary;
        Address = address;
        Kind = kind;
        retained = lease;
    }

    internal NativeOffheapHeader576(
        ProcessTermHeap506 owner,
        ProcessTermSequence506 block,
        AtomicsGuestRoot413 backing,
        nint binary,
        nint address,
        AtomicsBackingLease586 lease
    )
    {
        Owner = owner;
        Block = block;
        Atomics586 = backing;
        Binary = binary;
        Address = address;
        Kind = NativeOffheapKind576.MagicReference;
        atomicsRetained586 = lease;
    }

    internal IReadOnlyList<uint> ReadNumbers586()
        => Atomics586 is { } backing
            ? backing.MagicNumbers586(atomicsRetained586 ?? throw new ObjectDisposedException(nameof(NativeOffheapHeader576)))
            : Payload.ReadMagicNumbers576(Binary);

    internal void Release576(bool guestGc)
    {
        NativeResourceRootLease524? selected = Interlocked.Exchange(ref retained, null);
        if (guestGc)
            selected?.DisposeGuestGc536();
        else
            selected?.Dispose();
        Interlocked.Exchange(ref atomicsRetained586, null)?.Dispose();
    }
}
