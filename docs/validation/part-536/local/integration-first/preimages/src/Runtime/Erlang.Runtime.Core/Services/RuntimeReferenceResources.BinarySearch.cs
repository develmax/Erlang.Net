namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreateBinarySearchPattern(BinarySearchPattern pattern)
        => Create(ReferenceResourceKind.BinarySearchPattern, pattern);

    internal ReferenceResourceLease<BinarySearchPattern>? AcquireBinarySearchPattern(ReferenceTerm reference)
        => Acquire<BinarySearchPattern>(reference, ReferenceResourceKind.BinarySearchPattern);
}
