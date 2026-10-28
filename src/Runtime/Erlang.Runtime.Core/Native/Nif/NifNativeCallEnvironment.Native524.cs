namespace Erlang;

internal sealed unsafe partial class NifNativeCallEnvironment
{
    private HashSet<Term>? nativeStaticRoots524;
    private List<IDisposable>? nativeLeases524;

    private void RetainNativeValue524(NifNativeTermValue value)
    {
        if (!NativeResourceAddresses524.AnyLive524) return;
        var seen = new HashSet<NifNativeTermNode>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<NifNativeTermValue>();
        pending.Push(value);
        while (pending.TryPop(out NifNativeTermValue next))
        {
            if (next.StaticTerm is { } term)
            {
                nativeStaticRoots524 ??= new(ReferenceEqualityComparer.Instance);
                if (nativeStaticRoots524.Contains(term)) continue;
                IDisposable? lease = NativeResourceRoots524.RetainGraph524(term, Runtime);
                if (lease is not null)
                {
                    try
                    {
                        (nativeLeases524 ??= new()).Add(lease);
                    }
                    catch
                    {
                        lease.Dispose();
                        throw;
                    }
                }
                // Cache only immutable static terms. Mutable pending graphs are
                // traversed anew, so a later map mutation cannot hide a new root.
                nativeStaticRoots524.Add(term);
            }
            else if (next.Pending is { } node && seen.Add(node))
                for (int index = node.ChildCount - 1; index >= 0; index--)
                    pending.Push(node.Child(index));
        }
    }

    private void ClearNativeRoots524()
    {
        nativeStaticRoots524?.Clear();
        if (nativeLeases524 is not { } leases) return;
        var failures = new List<Exception>(leases.Count);
        foreach (IDisposable lease in leases)
            try
            {
                lease.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        leases.Clear();
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
