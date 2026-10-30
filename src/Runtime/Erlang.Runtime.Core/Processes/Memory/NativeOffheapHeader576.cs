namespace Erlang;

// This record owns the real backing lease of ONE actual registered three-word
// wrapper. It is not an alternative identity or a counted shadow allocation.
internal sealed class NativeOffheapHeader576
{
    private OrdinaryBinaryLease? ordinaryLease;
    internal OrdinaryBinaryBacking? OrdinaryBinary { get; }
    private NativeResourceRootLease524? retained;
    private AtomicsBackingLease586? atomicsRetained586;
    private CountersBackingLease596? countersRetained596;
    internal CountersGuestRoot421? Counters596 { get; }
    private BinaryProgramLease598? binaryRetained598;
    internal BinaryProgramBacking598? Binary598 { get; }
    private ReMagicLease597? regexRetained597;
    internal ReMagicBacking597? Regex597 { get; }
    private readonly NativeResourcePayload524? payload;
    internal AtomicsGuestRoot413? Atomics586 { get; }
    internal ProcessTermHeap506 Owner { get; }
    internal ProcessTermSequence506 Block { get; }
    internal NativeResourcePayload524 Payload => payload ?? throw new InvalidOperationException(MagicReference576Names.Invalid);
    private readonly nint binarySnapshot;
    internal nint Binary => OrdinaryBinary?.BinaryPointer ?? binarySnapshot;
    internal nint Address { get; }
    internal NativeOffheapKind576 Kind { get; }
    internal ResourceReferenceTerm? MagicTerm { get; set; }
    internal bool Published { get; set; }
    private PhysicalNodeLease? externalNode;
    internal PhysicalNodeLease? ExternalNode => externalNode;

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
        binarySnapshot = binary;
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
        binarySnapshot = binary;
        Address = address;
        Kind = NativeOffheapKind576.MagicReference;
        atomicsRetained586 = lease;
    }

    internal NativeOffheapHeader576(
        ProcessTermHeap506 owner,
        ProcessTermSequence506 block,
        CountersGuestRoot421 backing,
        nint binary,
        nint address,
        CountersBackingLease596 lease
    )
    {
        Owner = owner;
        Block = block;
        Counters596 = backing;
        binarySnapshot = binary;
        Address = address;
        Kind = NativeOffheapKind576.MagicReference;
        countersRetained596 = lease;
    }

    internal NativeOffheapHeader576(
        ProcessTermHeap506 owner,
        ProcessTermSequence506 block,
        ReMagicBacking597 backing,
        nint binary,
        nint address,
        ReMagicLease597 lease
    )
    {
        Owner = owner;
        Block = block;
        Regex597 = backing;
        binarySnapshot = binary;
        Address = address;
        Kind = NativeOffheapKind576.MagicReference;
        regexRetained597 = lease;
    }

    internal NativeOffheapHeader576(
        ProcessTermHeap506 owner,
        ProcessTermSequence506 block,
        BinaryProgramBacking598 backing,
        nint binary,
        nint address,
        BinaryProgramLease598 lease
    )
    {
        Owner = owner;
        Block = block;
        Binary598 = backing;
        binarySnapshot = binary;
        Address = address;
        Kind = NativeOffheapKind576.MagicReference;
        binaryRetained598 = lease;
    }

    internal NativeOffheapHeader576(
        ProcessTermHeap506 owner,
        ProcessTermSequence506 block,
        OrdinaryBinaryBacking binary,
        nint address,
        OrdinaryBinaryLease lease
    )
    {
        Owner = owner;
        Block = block;
        OrdinaryBinary = binary;
        binarySnapshot = binary.BinaryPointer;
        Address = address;
        Kind = NativeOffheapKind576.BinaryReference;
        ordinaryLease = lease;
    }

    internal NativeOffheapHeader576(
        ProcessTermHeap506 owner,
        ProcessTermSequence506 block,
        NativeOffheapKind576 kind,
        nint address,
        PhysicalNodeLease lease
    )
    {
        if (!ExternalIdentifierLayout.IsKind(kind)) throw new InvalidOperationException(ExternalIdentifierLayout.Invalid);
        Owner = owner;
        Block = block;
        Kind = kind;
        Address = address;
        binarySnapshot = lease.Address;
        externalNode = lease;
    }

    internal IReadOnlyList<uint> ReadNumbers586()
        => Binary598 is { } binary
            ? binary.Numbers598(binaryRetained598 ?? throw new ObjectDisposedException(nameof(NativeOffheapHeader576)))
            : Regex597 is { } regex
            ? regex.Numbers597(regexRetained597 ?? throw new ObjectDisposedException(nameof(NativeOffheapHeader576)))
            : Atomics586 is { } backing
            ? backing.MagicNumbers586(atomicsRetained586 ?? throw new ObjectDisposedException(nameof(NativeOffheapHeader576)))
            : Counters596 is { } counters
                ? counters.MagicNumbers596(countersRetained596 ?? throw new ObjectDisposedException(nameof(NativeOffheapHeader576)))
                : Payload.ReadMagicNumbers576(Binary);

    internal void Release576(bool guestGc)
    {
        try
        {
            NativeResourceRootLease524? selected = Interlocked.Exchange(ref retained, null);
            if (guestGc)
                selected?.DisposeGuestGc536();
            else
                selected?.Dispose();
            Interlocked.Exchange(ref atomicsRetained586, null)?.Dispose();
            Interlocked.Exchange(ref countersRetained596, null)?.Dispose();
            try
            {
                Interlocked.Exchange(ref regexRetained597, null)?.Dispose();
            }
            finally
            {
                try
                {
                    Interlocked.Exchange(ref binaryRetained598, null)?.Dispose();
                }
                finally
                {
                    try
                    {
                        OrdinaryBinary?.DetachWritable(this);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref ordinaryLease, null)?.Dispose();
                    }
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref externalNode, null)?.Dispose();
        }
    }
}
