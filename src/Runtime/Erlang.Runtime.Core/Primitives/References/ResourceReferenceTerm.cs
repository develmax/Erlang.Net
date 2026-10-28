namespace Erlang;

public sealed class ResourceReferenceTerm : ReferenceTerm, IReferenceResourceSerialization
{
    internal ResourceReferenceTerm(ReferenceResourceEntry entry)
        : base(entry.Identity)
    {
        Entry = entry;
    }

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ProcessTermSequence506 storage)
        : base(entry.Identity, storage)
    {
        Entry = entry;
    }

    internal ResourceReferenceTerm(ReferenceResourceEntry entry, ReferenceTerm portableIdentity)
        : base(portableIdentity)
    {
        Entry = entry;
    }

    internal ReferenceResourceEntry Entry { get; }
    public ReferenceResourceKind ResourceKind => Entry.Kind;

    public void SaveReferenceIdentity() => Entry.Owner.Save(Entry);
}
