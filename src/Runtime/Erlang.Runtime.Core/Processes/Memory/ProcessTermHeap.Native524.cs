namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private readonly Dictionary<Term, ProcessNativeHeader524> nativeHeaders524 = new(ReferenceEqualityComparer.Instance);
    private long nativeIdentity524, nativeHighWater524;
    internal ProcessRuntime Runtime524 => runtime;

    // Caller owns Gate and keeps the complete input graph rooted during admission.
    internal ProcessNativePublication524? PrepareNativeHeaders524(Term graph)
    {
        if (!NativeResourceAddresses524.AnyLive524)
            return null;
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        var targets = new List<(Term Term, NativeResourcePayload524 Payload)>();
        pending.Push(graph);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            if (NativeResourceRoots524.Direct524(value) is { } payload && !nativeHeaders524.ContainsKey(value))
            {
                if (!ReferenceEquals(runtime, payload.Runtime))
                    throw new NotSupportedException(NativeResource524Names.Foreign);
                targets.Add((value, payload));
            }
            foreach (Term child in NativeMapOwnedRoots510.Children(value)) pending.Push(child);
        }
        if (targets.Count == 0) return null;
        nativeHeaders524.EnsureCapacity(checked(nativeHeaders524.Count + targets.Count));
        var inserted = new ProcessNativeHeader524[targets.Count];
        var publication = new ProcessNativePublication524(this, inserted);
        int admitted = 0;
        try
        {
            foreach (var target in targets)
            {
                long next = checked(nativeIdentity524 + 1);
                IDisposable lease = target.Payload.RetainRoot524();
                ProcessNativeHeader524 header;
                try
                {
                    header = new(next, target.Term, lease);
                }
                catch
                {
                    lease.Dispose();
                    throw;
                }
                try
                {
                    nativeHeaders524.Add(target.Term, header);
                }
                catch
                {
                    header.Dispose();
                    throw;
                }
                inserted[admitted++] = header;
                nativeIdentity524 = next;
            }

            return publication;
        }
        catch
        {
            publication.Dispose();
            throw;
        }
    }

    internal void RollbackNativeHeaders524(ProcessNativeHeader524[] inserted)
    {
        foreach (ProcessNativeHeader524? header in inserted)
        {
            if (header is null) continue;
            nativeHeaders524.Remove(header.Term524);
            header.Dispose();
        }
    }

    // Preparation may allocate; no lease or generation state changes yet. The
    // marked set is supplied before common dead-block enumeration for 526.
    private ProcessNativeCollection524? PrepareNativeCollection524(HashSet<Term> seen, bool major, HashSet<long> marked)
    {
        if (nativeHeaders524.Count == 0) return null;
        ProcessNativeHeader524[] dead = nativeHeaders524.Values
            .Where(header => !seen.Contains(header.Term524) && (major || !header.Old524)).ToArray();
        ProcessNativeHeader524[] survivors = nativeHeaders524.Values
            .Where(header => seen.Contains(header.Term524) || (!major && header.Old524)).ToArray();

        // 526 must mark each survivor's actual raw-header block here, including
        // unseen old headers. 524 has no such numeric allocation and invents none.

        return new(
            this,
            dead,
            survivors,
            major,
            nativeIdentity524,
            nativeHighWater524
        );
    }

    internal void CommitNativeCollection524(
        ProcessNativeHeader524[] dead,
        ProcessNativeHeader524[] survivors,
        bool major,
        long nextHighWater,
        long previousHighWater
    )
    {
        foreach (ProcessNativeHeader524 header in dead)
        {
            nativeHeaders524.Remove(header.Term524);
            header.DisposeGuestGc536();
        }
        foreach (ProcessNativeHeader524 header in survivors)
            header.Old524 = !major && (header.Old524 || header.Identity524 <= previousHighWater);
        nativeHighWater524 = nextHighWater;
    }

    private void RetireNativeHeaders524()
    {
        foreach (ProcessNativeHeader524 header in nativeHeaders524.Values) header.DisposeGuestGc536();
        nativeHeaders524.Clear();
        nativeHighWater524 = nativeIdentity524;
    }
}
