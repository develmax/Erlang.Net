namespace Erlang;

internal sealed partial class RuntimeEtsStoredObject
{
    internal void CaptureNodeDistReferences(NodeDistReferenceSnapshot snapshot, Term owner)
    {
        var selected = Volatile.Read(ref payload) ?? throw new ObjectDisposedException(nameof(RuntimeEtsStoredObject));
        selected.CaptureNodeDistReferences(snapshot, owner);
    }
}
