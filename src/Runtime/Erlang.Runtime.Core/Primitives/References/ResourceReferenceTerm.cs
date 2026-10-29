namespace Erlang;

public sealed class ResourceReferenceTerm : ReferenceTerm, IReferenceResourceSerialization
{
    internal ResourceReferenceTerm(ReferenceResourceEntry entry)
        : base(entry.Identity)
    {
        Entry = entry;
        AtomicsBacking586 = ReadAtomicsBacking586(entry);
        CountersBacking596 = ReadCountersBacking596(entry);
    }

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ProcessTermSequence506 storage)
        : base(entry.Identity, storage)
    {
        Entry = entry;
        AtomicsBacking586 = ReadAtomicsBacking586(entry);
        CountersBacking596 = ReadCountersBacking596(entry);
    }

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ReferenceTerm portableIdentity)
        : base(portableIdentity)
    {
        Entry = entry;
        AtomicsBacking586 = ReadAtomicsBacking586(entry);
        CountersBacking596 = ReadCountersBacking596(entry);
    }

    internal ReferenceResourceEntry Entry { get; }
    internal AtomicsGuestRoot413? AtomicsBacking586 { get; }
    internal AtomicsHostCustody586? AtomicsHost586 { get; }

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

    public ReferenceResourceKind ResourceKind => Entry.Kind;

    public void SaveReferenceIdentity() => Entry.Owner.Save(Entry);
}
