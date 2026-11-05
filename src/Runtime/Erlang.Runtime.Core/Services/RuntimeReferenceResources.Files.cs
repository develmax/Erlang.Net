namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreateFile(PrimFileDataResource resource) => Create(ReferenceResourceKind.PrimFile, resource);

    internal ReferenceResourceLease<PrimFileDataResource>? AcquireFile(ReferenceTerm reference)
        => Acquire<PrimFileDataResource>(reference, ReferenceResourceKind.PrimFile);

    internal ResourceReferenceTerm CreateBuffer(PrimBufferDataResource resource) => Create(ReferenceResourceKind.PrimBuffer, resource);

    internal ReferenceResourceLease<PrimBufferDataResource>? AcquireBuffer(ReferenceTerm reference)
        => Acquire<PrimBufferDataResource>(reference, ReferenceResourceKind.PrimBuffer);
}
