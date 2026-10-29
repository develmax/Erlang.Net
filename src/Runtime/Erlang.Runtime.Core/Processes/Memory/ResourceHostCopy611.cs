namespace Erlang;

// Public callbacks and snapshots must export every supported magic-resource
// family through owning Host copies, including counters-only message graphs.
internal static class ResourceHostCopy611
{
    internal static bool Requires(Term value)
        => NativeResourceRoots524.Contains524(value)
            || AtomicsRoots586.Contains586(value)
            || CountersRoots596.Contains596(value);
}
