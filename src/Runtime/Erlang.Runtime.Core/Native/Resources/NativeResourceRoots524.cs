namespace Erlang;

internal static class NativeResourceRoots524
{
    internal static NativeResourcePayload524? Direct524(Term value, ProcessRuntime? expected = null)
    {
        // Admit backing custody before a live/stale descriptor can read its heap.
        NativeResourcePayload524? payload = value is BitString { ResourceBacking577.NativeStorage524.ResourceIdentity524: NativeResourcePayload524 binary }
            ? binary : value is ResourceReferenceTerm resource && resource.ResourceKind == ReferenceResourceKind.NativeResource524
            ? resource.Entry.Read<NativeResourcePayload524>() : null;
        if (payload is null)
            return null;
        if (expected is not null && !ReferenceEquals(payload.Runtime, expected))
            throw new NotSupportedException(NativeResource524Names.Foreign);
        if (value is BitString bits)
            _ = bits.NativeResourceIdentity524;
        if (value is ResourceReferenceTerm { OwnedStorage562: { } storage } owned)
        {
            if (storage is not ProcessTermSequence506 || storage.Owner is not ProcessTermHeap506 heap)
                throw new InvalidOperationException(MagicReference576Names.Invalid);

            return heap.ReadMagicPayload576(owned, payload);
        }

        return payload;
    }

    internal static bool Contains524(Term source)
    {
        if (!NativeResourceAddresses524.AnyLive524) return false;
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(source);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            if (Direct524(value) is not null) return true;
            foreach (Term child in NativeMapOwnedRoots510.Children(value)) pending.Push(child);
        }

        return false;
    }

    internal static IDisposable? RetainGraph524(Term source, ProcessRuntime? expected = null, bool skipOwnedMagic576 = false)
    {
        if (!NativeResourceAddresses524.AnyLive524) return null;
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var payloads = new HashSet<NativeResourcePayload524>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(source);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            if (Direct524(value, expected) is { } payload
                && (!skipOwnedMagic576 || !ProcessTermHeap506.HasOwnedMagicReference576(value)))
            {
                if (expected is not null && !ReferenceEquals(expected, payload.Runtime))
                    throw new NotSupportedException(NativeResource524Names.Foreign);
                payloads.Add(payload);
            }
            foreach (Term child in NativeMapOwnedRoots510.Children(value)) pending.Push(child);
        }
        if (payloads.Count == 0) return null;
        var leases = new IDisposable[payloads.Count];
        int admitted = 0;
        try
        {
            foreach (NativeResourcePayload524 payload in payloads)
                leases[admitted++] = payload.RetainRoot524();

            return new NativeResourceRootSet524(leases);
        }
        catch
        {
            for (int index = admitted - 1; index >= 0; index--) leases[index].Dispose();
            throw;
        }
    }
}
