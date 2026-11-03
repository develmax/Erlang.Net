namespace Erlang;

internal static class NativeResourceRoots524
{
    internal static void DisposeGuestGraph593(IDisposable? selected)
    {
        if (selected is NativeResourceRootLease524 native) native.DisposeGuestGc536();
        else if (selected is NativeResourceRootSet524 set) set.DisposeGuestGc593();
        else selected?.Dispose();
    }

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
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (Direct524(value) is not null) return true;
            traversal.EnqueueChildren(value);
        }

        return false;
    }

    internal static IDisposable? RetainGraph524(
        Term source,
        ProcessRuntime? expected = null,
        bool skipOwnedMagic576 = false,
        bool hostAtomics586 = false
    )
    {
        IDisposable? regex = ReMagicRoots597.RetainGraph597(
            source,
            expected,
            skipOwnedMagic576,
            hostAtomics586
        );
        IDisposable? existing = null;
        try
        {
            existing = RetainExistingGraph597(
                source,
                expected,
                skipOwnedMagic576,
                hostAtomics586
            );
            if (regex is null) return existing;
            if (existing is null) return regex;

            return new NativeResourceRootSet524([regex, existing]);
        }
        catch (Exception original)
        {
            Exception? first = null, second = null;
            try
            {
                existing?.Dispose();
            }
            catch (Exception error)
            {
                first = error;
            }
            try
            {
                regex?.Dispose();
            }
            catch (Exception error)
            {
                second = error;
            }
            if (first is not null || second is not null)
                throw new AggregateException(new[] { original, first, second }.OfType<Exception>());
            throw;
        }
    }

    private static IDisposable? RetainExistingGraph597(
        Term source,
        ProcessRuntime? expected = null,
        bool skipOwnedMagic576 = false,
        bool hostAtomics586 = false
    )
    {
        var retained = new IDisposable[3];
        int count = 0;
        try
        {
            if (AtomicsRoots586.RetainGraph586(
                source,
                expected,
                skipOwnedMagic576,
                hostAtomics586
            ) is { } atomics)
                retained[count++] = atomics;
            if (CountersRoots596.RetainGraph596(
                source,
                expected,
                skipOwnedMagic576,
                hostAtomics586
            ) is { } counters)
                retained[count++] = counters;
            if (RetainNativeGraph524(source, expected, skipOwnedMagic576) is { } native)
                retained[count++] = native;
            if (count == 0)
                return null;
            if (count == 1)
                return retained[0];

            return new NativeResourceRootSet524(retained[..count]);
        }
        catch (Exception original)
        {
            // Independent typed acquisitions are rolled back without callback under a registry gate.
            Exception? firstCleanup = null, secondCleanup = null, thirdCleanup = null;
            for (int index = count - 1; index >= 0; index--)
                try
                {
                    retained[index].Dispose();
                }
                catch (Exception failure)
                {
                    if (firstCleanup is null) firstCleanup = failure;
                    else if (secondCleanup is null) secondCleanup = failure;
                    else thirdCleanup = failure;
                }
            if (thirdCleanup is not null)
                throw new AggregateException(
                    original,
                    firstCleanup!,
                    secondCleanup!,
                    thirdCleanup
                );
            if (secondCleanup is not null)
                throw new AggregateException(original, firstCleanup!, secondCleanup);
            if (firstCleanup is not null)
                throw new AggregateException(original, firstCleanup);
            throw;
        }
    }

    private static IDisposable? RetainNativeGraph524(Term source, ProcessRuntime? expected, bool skipOwnedMagic576)
    {
        if (!NativeResourceAddresses524.AnyLive524) return null;
        var payloads = new HashSet<NativeResourcePayload524>(ReferenceEqualityComparer.Instance);
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (Direct524(value, expected) is { } payload
                && (!skipOwnedMagic576 || !ProcessTermHeap506.HasOwnedMagicReference576(value)))
            {
                if (expected is not null && !ReferenceEquals(expected, payload.Runtime))
                    throw new NotSupportedException(NativeResource524Names.Foreign);
                payloads.Add(payload);
            }
            traversal.EnqueueChildren(value);
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
