namespace Erlang;

public sealed class ResourceReferenceTerm : ReferenceTerm, IReferenceResourceSerialization
{
    internal ResourceReferenceTerm(ReferenceResourceEntry entry)
        : base(entry.Identity.Node, entry.Identity.Words, entry.Identity.Creation)
    {
        Entry = entry;
    }

    internal ReferenceResourceEntry Entry { get; }
    public ReferenceResourceKind ResourceKind => Entry.Kind;

    public void SaveReferenceIdentity() => Entry.Owner.Save(Entry);
}
