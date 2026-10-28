namespace Erlang;

internal static class NativeResourceRoots524
{
    internal static NativeResourcePayload524? Direct524(Term value)
    {
        if (value is BitString { NativeResourceIdentity524: NativeResourcePayload524 binary })
            return binary;
        if (value is ResourceReferenceTerm resource && resource.ResourceKind == ReferenceResourceKind.NativeResource524)
            return resource.Entry.Read<NativeResourcePayload524>();

        return null;
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

    internal static IDisposable? RetainGraph524(Term source, ProcessRuntime? expected = null)
    {
        if (!NativeResourceAddresses524.AnyLive524) return null;
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var payloads = new HashSet<NativeResourcePayload524>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(source);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            if (Direct524(value) is { } payload)
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
