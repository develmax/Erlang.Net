namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreateAtomics413(AtomicsGuestRoot413 root)
    {
        ResourceReferenceTerm reference = Create(ReferenceResourceKind.Atomics413, root);
        root.Array.BindIdentity(reference.Words);

        return reference;
    }

    internal ReferenceResourceLease<AtomicsGuestRoot413>? AcquireAtomics413(ReferenceTerm reference)
        => Acquire<AtomicsGuestRoot413>(reference, ReferenceResourceKind.Atomics413);
}
