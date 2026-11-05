namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    // These existing interop payloads are authenticated managed references,
    // unlike atomics/regex/native resources with explicit off-heap leases.
    // Their weak identity index must observe retirement after a guest GC too.
    internal bool HasManagedCollectionPayloads
    {
        get
        {
            lock (gate)
            {
                RemoveDead();

                return entries.Values.Any(weak => weak.TryGetTarget(out var entry) && entry.IsAlive
                    && entry.Kind is ReferenceResourceKind.CompiledMatchSpecification419
                        or ReferenceResourceKind.PrimFile or ReferenceResourceKind.PrimBuffer);
            }
        }
    }
}
