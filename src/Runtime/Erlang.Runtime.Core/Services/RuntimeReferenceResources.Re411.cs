namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreateRegex411(RePattern411 pattern) => Create(ReferenceResourceKind.Regex411, pattern);

    internal ReferenceResourceLease<RePattern411>? AcquireRegex411(ReferenceTerm reference) => Acquire<RePattern411>(reference, ReferenceResourceKind.Regex411);
}
