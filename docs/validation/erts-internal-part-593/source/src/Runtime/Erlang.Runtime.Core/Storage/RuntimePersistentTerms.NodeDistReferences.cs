namespace Erlang;

internal sealed partial class RuntimePersistentTerms
{
    internal void CaptureNodeDistReferences(NodeDistReferenceSnapshot snapshot)
    {
        using var read = arena.AcquireSnapshot();
        read.CaptureNodeDistReferences(snapshot);
    }
}
