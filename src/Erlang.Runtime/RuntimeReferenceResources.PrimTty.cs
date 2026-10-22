namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreatePrimTty(PrimTtyResourceRoot root)
        => Create(ReferenceResourceKind.PrimTty, root);

    internal ReferenceResourceLease<PrimTtyResourceRoot>? AcquirePrimTty(ReferenceTerm reference)
        => Acquire<PrimTtyResourceRoot>(reference, ReferenceResourceKind.PrimTty);
}
