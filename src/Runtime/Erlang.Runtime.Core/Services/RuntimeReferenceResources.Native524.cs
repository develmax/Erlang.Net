namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    // Strong identity index, NOT a destructor/GC reachability oracle. Explicit
    // native/guest/store roots alone control the payload's real refcount.
    private readonly Dictionary<NativeResourcePayload524, ReferenceResourceEntry> native524 = new(ReferenceEqualityComparer.Instance);

    internal ResourceReferenceTerm MakeNativeResource524(NativeResourcePayload524 payload)
    {
        payload.ValidateRead524();
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, NativeResource524Names.Closed);
            if (native524.TryGetValue(payload, out ReferenceResourceEntry? existing))
                return new ResourceReferenceTerm(existing);
            native524.EnsureCapacity(checked(native524.Count + 1));
            ResourceReferenceTerm created = Create(ReferenceResourceKind.NativeResource524, payload);
            native524.Add(payload, created.Entry);

            return created;
        }
    }

    internal void ForgetNativeResource524(NativeResourcePayload524 payload)
    {
        lock (gate)
        {
            if (!native524.Remove(payload, out ReferenceResourceEntry? entry))
                return;
            entries.Remove(entry.Identity);
            entry.Close();
        }
    }
}
