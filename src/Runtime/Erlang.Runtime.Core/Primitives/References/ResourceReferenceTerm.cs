namespace Erlang;

public sealed class ResourceReferenceTerm : ReferenceTerm, IReferenceResourceSerialization
{
    internal ResourceReferenceTerm(ReferenceResourceEntry entry)
        : base(entry.Identity)
    {
        Entry = entry;
        AtomicsBacking586 = ReadAtomicsBacking586(entry);
        CountersBacking596 = ReadCountersBacking596(entry);
        RegexBacking597 = ReadRegexBacking597(entry);
        BinaryBacking598 = ReadBinaryBacking598(entry);
        PreparedBacking807 = ReadPreparedBacking807(entry);
    }

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ProcessTermSequence506 storage)
        : base(entry.Identity, storage)
    {
        Entry = entry;
        AtomicsBacking586 = ReadAtomicsBacking586(entry);
        RegexBacking597 = ReadRegexBacking597(entry);
        CountersBacking596 = ReadCountersBacking596(entry);
        BinaryBacking598 = ReadBinaryBacking598(entry);
        PreparedBacking807 = ReadPreparedBacking807(entry);
    }

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ReferenceTerm portableIdentity)
        : base(portableIdentity)
    {
        Entry = entry;
        AtomicsBacking586 = ReadAtomicsBacking586(entry);
        CountersBacking596 = ReadCountersBacking596(entry);
        RegexBacking597 = ReadRegexBacking597(entry);
        BinaryBacking598 = ReadBinaryBacking598(entry);
        PreparedBacking807 = ReadPreparedBacking807(entry);
    }

    internal ReferenceResourceEntry Entry { get; }
    internal AtomicsGuestRoot413? AtomicsBacking586 { get; }
    internal AtomicsHostCustody586? AtomicsHost586 { get; }
    internal ReMagicBacking597? RegexBacking597 { get; }
    internal ReMagicHost597? RegexHost597 { get; }

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ReferenceTerm identity, ReMagicHost597 host)
        : this(entry, identity)
    {
        RegexHost597 = host;
    }

    private static ReMagicBacking597? ReadRegexBacking597(ReferenceResourceEntry entry)
        => entry.Kind == ReferenceResourceKind.Regex411 ? entry.Read<RePattern411>().Backing597 : null;

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ReferenceTerm identity, AtomicsHostCustody586 host)
        : this(entry, identity)
    {
        AtomicsHost586 = host;
    }

    private static AtomicsGuestRoot413? ReadAtomicsBacking586(ReferenceResourceEntry entry)
        => entry.Kind == ReferenceResourceKind.Atomics413 ? entry.Read<AtomicsGuestRoot413>() : null;

    internal CountersGuestRoot421? CountersBacking596 { get; }
    internal CountersHostCustody596? CountersHost596 { get; }

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ReferenceTerm identity, CountersHostCustody596 host)
        : this(entry, identity)
    {
        CountersHost596 = host;
    }

    private static CountersGuestRoot421? ReadCountersBacking596(ReferenceResourceEntry entry)
        => entry.Kind == ReferenceResourceKind.Counters421 ? entry.Read<CountersGuestRoot421>() : null;

    internal BinaryProgramBacking598? BinaryBacking598 { get; }
    internal BinaryProgramHost598? BinaryHost598 { get; }

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ReferenceTerm identity, BinaryProgramHost598 host)
        : this(entry, identity)
    {
        BinaryHost598 = host;
    }

    private static BinaryProgramBacking598? ReadBinaryBacking598(ReferenceResourceEntry entry)
        => entry.Kind == ReferenceResourceKind.BinarySearchPattern ? entry.Read<BinarySearchPattern>().Backing598 : null;

    internal PreparedLoaderOwner807? PreparedBacking807 { get; }
    internal PreparedLoaderHost807? PreparedHost807 { get; }

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ReferenceTerm identity, PreparedLoaderHost807 host)
        : this(entry, identity)
    {
        PreparedHost807 = host;
    }

    private static PreparedLoaderOwner807? ReadPreparedBacking807(ReferenceResourceEntry entry)
        => entry.Kind == ReferenceResourceKind.PreparedBeam ? entry.Read<PreparedLoaderOwner807>() : null;

    public ReferenceResourceKind ResourceKind => Entry.Kind;

    public void SaveReferenceIdentity() => Entry.Owner.Save(Entry);
}
