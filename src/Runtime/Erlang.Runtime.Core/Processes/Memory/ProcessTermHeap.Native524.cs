namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private readonly Dictionary<Term, ProcessNativeHeader524> nativeHeaders524 = new(ReferenceEqualityComparer.Instance);
    private long nativeIdentity524, nativeHighWater524;
    internal ProcessRuntime Runtime524 => runtime;

    // Caller owns Gate and keeps the complete input graph rooted during admission.
    internal ProcessNativePublication524? PrepareNativeHeaders524(Term graph, bool legacyHeaders576 = true)
    {
        if (!NativeResourceAddresses524.AnyLive524 && magicReferences576.Count == 0 && functionEntries.Count == 0)
            return null;
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        var targets = new List<(Term Term, NativeResourcePayload524 Payload)>();
        var promotions = new List<NativeOffheapHeader576>();
        var binaryPromotions = new List<ResourceBinaryDescriptor577>();
        var functionPromotions = new List<ManagedFunctionDispatchEntry>();
        pending.Push(graph);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            // Certified immutable Cons/Tuple graphs with immediate leaves have no native headers
            // to promote. The actual live same-heap check precedes the shortcut.
            if (value is Cons or TupleTerm && Monitor.IsEntered(Gate)
                && Storage(value) is ProcessTermSequence506 { HasImmutablePlainClosure550: true }
                && IsOwnedLocal(value)) continue;
            if (value is ResourceReferenceTerm { OwnedStorage562: { } storage } && !ReferenceEquals(storage.Owner, this))
                throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
            if (value is FunctionTerm { OwnedFunctionStorage: ProcessTermSequence506 functionBlock })
            {
                var entry = RequireFunctionEntry(functionBlock, out _);
                if (!entry.Published) functionPromotions.Add(entry);
            }
            if (Storage(value) is ProcessTermSequence506 externalBlock
                && magicReferences576.TryGetValue(externalBlock.Identity, out var externalEntry)
                && ExternalIdentifierLayout.IsKind(externalEntry.Kind))
            {
                _ = RequireNativeOffheapHeader576(externalBlock, externalEntry.Kind);
                if (!externalEntry.Published) promotions.Add(externalEntry);
            }
            bool actualMagic = HasOwnedMagicReference576(value);
            if (actualMagic && value is ResourceReferenceTerm { OwnedStorage562: ProcessTermSequence506 block })
            {
                NativeOffheapHeader576 descriptor = RequireMagicReference576(block);
                if (!descriptor.Published)
                    promotions.Add(descriptor);
            }
            bool actualBinary = TryGetResourceBitsDescriptor577(value, out var resourceBits);
            if (actualBinary)
            {
                if (!resourceBits!.Published) binaryPromotions.Add(resourceBits);
                if (!resourceBits.BinRef.Published) promotions.Add(resourceBits.BinRef);
            }
            if (NativeResourceRoots524.Direct524(value, runtime) is { } payload
                && legacyHeaders576 && !actualMagic && !actualBinary && !nativeHeaders524.ContainsKey(value))
            {
                if (!ReferenceEquals(runtime, payload.Runtime))
                    throw new NotSupportedException(NativeResource524Names.Foreign);
                targets.Add((value, payload));
            }
            foreach (Term child in NativeMapOwnedRoots510.Children(value)) pending.Push(child);
        }
        if (targets.Count == 0 && promotions.Count == 0 && binaryPromotions.Count == 0 && functionPromotions.Count == 0) return null;
        nativeHeaders524.EnsureCapacity(checked(nativeHeaders524.Count + targets.Count));
        var inserted = new ProcessNativeHeader524[targets.Count];
        var publication = new ProcessNativePublication524(
            this,
            inserted,
            promotions.ToArray(),
            binaryPromotions.ToArray(),
            functionPromotions.ToArray()
        );
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
