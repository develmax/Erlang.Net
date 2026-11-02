namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // A real process block can retain literal child pointers after its last
    // frame disappears (in particular an unreachable OLD block before major GC).
    // This custody retires with actual physical block reclamation, not liveness
    // estimates and not CLR reachability of the Term facade.
    private readonly Dictionary<long, ModuleLiteralRoots801> literalBlockRoots801 = new();

    // Retain the genuine immutable block sidecar while H stabilizes both its
    // active native identity and its actual ownership interest. Missing sidecar
    // is NOT an empty-graph proof: caller must retain the original full walk.
    internal ModuleLiteralRoots801? RetainCertifiedLiteralBlock134(Term value)
    {
        lock (Gate)
        {
            if (value is not (Cons or TupleTerm or FunctionTerm or MapTerm or HamtBitmapTerm429) || Storage(value) is not ProcessTermSequence506 block)
                return null;
            _ = IsOwnedLocal(value);
            if (!CanUseImmutableClosure293(value, block)) return null;
            if ((!block.HasImmutableLiteralOnlyClosure132 && !block.HasImmutableReadonlyCaptureClosure138
                    && !block.HasImmutableLiteralEdges397)
                || block.IsMutableSequenceToken814)
                return null;

            return literalBlockRoots801.TryGetValue(block.Identity, out ModuleLiteralRoots801? roots)
                ? roots.Retain801() : null;
        }
    }

    private void ReleaseLiteralBlock801(long identity)
    {
        if (literalBlockRoots801.Remove(identity, out ModuleLiteralRoots801? roots)) roots.Dispose();
    }

    private void ReleaseAllLiteralBlocks801()
    {
        ModuleLiteralRoots801[] old = literalBlockRoots801.Values.ToArray();
        literalBlockRoots801.Clear();
        List<Exception>? failures = null;
        foreach (ModuleLiteralRoots801 item in old)
            try
            {
                item.Dispose();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        if (failures is not null) throw new AggregateException(failures);
    }
}
