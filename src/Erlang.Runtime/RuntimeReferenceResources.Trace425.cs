namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreateTraceSession425(TraceSessionHandle425 handle)
        => Create(ReferenceResourceKind.TraceSession425, handle);

    internal ReferenceResourceLease<TraceSessionHandle425>? AcquireTraceSession425(ReferenceTerm reference)
        => Acquire<TraceSessionHandle425>(reference, ReferenceResourceKind.TraceSession425);
}
