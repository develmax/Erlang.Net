namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private readonly Dictionary<long, NativeOffheapHeader576> magicReferences576 = new();
    private readonly Dictionary<nint, NativeOffheapHeader576> magicAddresses576 = new();
    private nint magicOffHeap576;

    internal bool IsNativeMagicReference576(Term value)
        => value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.NativeResource524 or ReferenceResourceKind.Atomics413 or ReferenceResourceKind.Counters421 or ReferenceResourceKind.Regex411 };

    internal static bool HasOwnedMagicReference576(Term value)
        => value is ResourceReferenceTerm { OwnedStorage562: ProcessTermSequence506 block }
            && block.Owner is ProcessTermHeap506 heap && heap.IsMagicBlock576(block);

    private bool IsMagicBlock576(ProcessTermSequence506 block)
    {
        lock (Gate)
            return magicReferences576.TryGetValue(block.Identity, out var owned) && ReferenceEquals(owned.Block, block) && owned.Kind == NativeOffheapKind576.MagicReference;
    }

    internal ResourceReferenceTerm CreateMagicReference576(ResourceReferenceTerm source, ProcessTermConstruction514 construction)
    {
        if (source.ResourceKind == ReferenceResourceKind.Counters421)
            return CreateCountersMagic596(source, construction);
        if (source.ResourceKind == ReferenceResourceKind.Regex411)
            return CreateRegexMagic597(source, construction);
        if (source.ResourceKind == ReferenceResourceKind.Atomics413)
            return CreateAtomicsMagic586(source, construction);
        if (source.ResourceKind != ReferenceResourceKind.NativeResource524
            || !ReferenceEquals(source.Entry.Owner, runtime.ReferenceResources))
            throw new NotSupportedException(NativeResource524Names.Foreign);
        NativeResourcePayload524 payload = source.Entry.Read<NativeResourcePayload524>();
        NativeOffheapHeader576 node = AllocateNativeOffheapHeader576(
            source,
            payload,
            NativeOffheapKind576.MagicReference,
            construction
        );
        try
        {
            var result = new ResourceReferenceTerm(source.Entry, node.Block);
            node.MagicTerm = result;
            construction.Publish(result);

            return result;
        }
        catch
        {
            RollbackNativeOffheapHeader576(node);
            throw;
        }
    }

    internal NativeOffheapHeader576 AllocateNativeOffheapHeader576(
        Term source,
        NativeResourcePayload524 payload,
        NativeOffheapKind576 kind,
        ProcessTermConstruction514 construction
    )
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        if (closing || !frames.Contains(construction.Frame517) || construction.Frame517.IsDisposed
            || !construction.Contains(source))
            throw new InvalidOperationException(MagicReference576Names.MissingCustody);
        if (Storage(source) is { } existing && !ReferenceEquals(existing.Owner, this))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        bool validSource = kind switch
        {
            NativeOffheapKind576.MagicReference => source is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.NativeResource524 },
            NativeOffheapKind576.BinaryReference => source is BitString,
            _ => false
        };
        if (!validSource || !ReferenceEquals(payload.Runtime, runtime)
            || !ReferenceEquals(NativeResourceRoots524.Direct524(source, runtime), payload))
            throw new NotSupportedException(NativeResource524Names.Foreign);
        RequireReferencePlatform562();
        construction.PrepareMagicTracking576();
        magicReferences576.EnsureCapacity(checked(magicReferences576.Count + 1));
        magicAddresses576.EnsureCapacity(checked(magicAddresses576.Count + 1));
        NativeResourceRootLease524? lease = payload.RetainRoot524();
        ProcessTermSequence506? block = null;
        NativeOffheapHeader576? owned = null;
        nint previous = magicOffHeap576;
        bool byIdentity = false, byAddress = false, linked = false;
        try
        {
            nint binary = payload.MagicBinaryPointer576();
            _ = payload.ReadMagicNumbers576(binary);
            if (binary == 0 || ((nuint)binary & (nuint)(IntPtr.Size - 1)) != 0
                || unchecked((uint)(nuint)binary) == OrdinaryReference562Names.Marker)
                throw new InvalidOperationException(MagicReference576Names.Invalid);
            block = AllocateEncoded526([], [NativeOffheap576Names.Header(kind)], [(nuint)binary, (nuint)previous]);
            nint address = CaptureRawAddress571(block, 0, NativeOffheap576Names.Words).Address571;
            owned = new(
                this,
                block,
                payload,
                binary,
                address,
                kind,
                lease
            );
            magicReferences576.Add(block.Identity, owned);
            byIdentity = true;
            magicAddresses576.Add(address, owned);
            byAddress = true;
            construction.TrackMagic576(owned);
            magicOffHeap576 = address;
            linked = true;
            lease = null;

            return owned;
        }
        catch
        {
            if (linked)
                magicOffHeap576 = previous;
            if (byAddress)
                magicAddresses576.Remove(owned!.Address);
            if (byIdentity)
                magicReferences576.Remove(block!.Identity);
            if (block is not null)
                ReleaseRegisteredHeader526(block);
            if (owned is not null)
                owned.Release576(false);
            else
                lease?.Dispose();
            throw;
        }
    }

    internal nint ReadNativeOffheapAddress576(ProcessTermSequence506 block, NativeOffheapKind576 kind)
        => RequireNativeOffheapHeader576(block, kind).Address;

    internal void RollbackNativeOffheapHeader576(NativeOffheapHeader576 node)
        => RollbackMagicReferences576([node]);

    private NativeOffheapHeader576 RequireMagicReference576(ProcessTermSequence506 block)
        => RequireNativeOffheapHeader576(block, NativeOffheapKind576.MagicReference);

    internal NativeOffheapHeader576 RequireNativeOffheapHeader576(ProcessTermSequence506 block, NativeOffheapKind576 kind)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        // All reads, including the actual prefix dereference, follow this
        // owner/identity admission. No foreign slab range is ever inspected.
        if (!ReferenceEquals(block.Owner, this))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        if (!blocks.TryGetValue(block.Identity, out var live) || !ReferenceEquals(live, block)
            || !magicReferences576.TryGetValue(block.Identity, out var owned) || !ReferenceEquals(owned.Block, block))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        if (owned.Kind != kind || block.Count != 0 || block.HeaderWords526 != NativeOffheap576Names.HeaderWords
            || block.RawPayloadWords556 != MagicReference576Names.DataWords
            || block.Slab.Cells.ReadHeader526(block.AllocationOffset526) != NativeOffheap576Names.Header(kind)
            || CaptureRawAddress571(block, 0, MagicReference576Names.Words).Address571 != owned.Address)
            throw new InvalidOperationException(MagicReference576Names.Invalid);
        nuint binary = block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + MagicReference576Names.BinaryWord);
        if (binary != (nuint)owned.Binary || binary == 0 || (binary & (nuint)(IntPtr.Size - 1)) != 0
            || unchecked((uint)binary) == OrdinaryReference562Names.Marker)
            throw new InvalidOperationException(MagicReference576Names.Invalid);
        nint next = (nint)block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + MagicReference576Names.NextWord);
        if (next == owned.Address || next != 0 && !magicAddresses576.ContainsKey(next))
            throw new InvalidOperationException(MagicReference576Names.Invalid);

        return owned;
    }

    private bool TryReadMagicWords576(ProcessTermSequence506 block, out IReadOnlyList<uint>? numbers)
    {
        numbers = null;
        if (!magicReferences576.TryGetValue(block.Identity, out var typed) || typed.Kind != NativeOffheapKind576.MagicReference)
            return false;
        NativeOffheapHeader576 owned = RequireMagicReference576(block);
        numbers = owned.ReadNumbers586();

        return true;
    }

    internal NativeResourcePayload524 ReadMagicPayload576(ResourceReferenceTerm term, NativeResourcePayload524 expected)
    {
        lock (Gate)
        {
            if (term.OwnedStorage562 is not ProcessTermSequence506 block)
                throw new InvalidOperationException(MagicReference576Names.Invalid);
            NativeOffheapHeader576 owned = RequireMagicReference576(block);
            if (!ReferenceEquals(owned.MagicTerm, term) || !ReferenceEquals(owned.Payload, expected))
                throw new InvalidOperationException(MagicReference576Names.Invalid);

            return owned.Payload;
        }
    }

    private MagicReferenceSweep576? PrepareMagicCollection576(HashSet<long> marked, bool major)
    {
        if (magicReferences576.Count == 0)
            return null;

        return PrepareMagicSweep576(node => !marked.Contains(node.Block.Identity)
            && (major || !oldBlocks508.Contains(node.Block.Identity)), false, marked);
    }

    internal void RollbackMagicReferences576(IReadOnlyList<NativeOffheapHeader576> candidates)
    {
        lock (Gate)
        {
            var discarded = new HashSet<NativeOffheapHeader576>(candidates.Where(node => !node.Published), ReferenceEqualityComparer.Instance);
            if (discarded.Count != 0)
                PrepareMagicSweep576(discarded.Contains, true, null)?.Commit576();
        }
    }

    private MagicReferenceSweep576? PrepareMagicSweep576(
        Func<NativeOffheapHeader576, bool> retire,
        bool rollback,
        HashSet<long>? marked
    )
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        var visited = new HashSet<nint>();
        var alive = new List<NativeOffheapHeader576>();
        var dead = new List<NativeOffheapHeader576>();
        nint cursor = magicOffHeap576;
        while (cursor != 0)
        {
            if (!visited.Add(cursor) || !magicAddresses576.TryGetValue(cursor, out var node))
                throw new InvalidOperationException(MagicReference576Names.Invalid);
            _ = RequireNativeOffheapHeader576(node.Block, node.Kind);
            nint next = (nint)node.Block.Slab.Cells.ReadNumeric556(node.Block.AllocationOffset526 + MagicReference576Names.NextWord);
            if (retire(node))
                dead.Add(node);
            else
            {
                alive.Add(node);
                marked?.Add(node.Block.Identity);
            }
            cursor = next;
        }
        if (visited.Count != magicReferences576.Count || visited.Count != magicAddresses576.Count)
            throw new InvalidOperationException(MagicReference576Names.Invalid);
        var links = new nuint[alive.Count][];
        for (int index = 0; index < alive.Count; index++)
            links[index] = [(nuint)(index + 1 == alive.Count ? 0 : alive[index + 1].Address)];

        return new(
            this,
            dead.ToArray(),
            alive.ToArray(),
            links,
            rollback
        );
    }

    internal void CommitMagicSweep576(
        NativeOffheapHeader576[] dead,
        NativeOffheapHeader576[] alive,
        nuint[][] links,
        bool rollback
    )
    {
        // All pointer/chain validation and link arrays precede commit.
        // Lease retirement only schedules physical C destruction; no native
        // destructor is invoked inline while this heap gate is held.
        for (int index = 0; index < alive.Length; index++)
            alive[index].Block.Slab.Cells.WriteNumeric556(links[index], alive[index].Block.AllocationOffset526 + MagicReference576Names.NextWord);
        magicOffHeap576 = alive.Length == 0 ? 0 : alive[0].Address;
        foreach (NativeOffheapHeader576 node in dead)
        {
            magicReferences576.Remove(node.Block.Identity);
            magicAddresses576.Remove(node.Address);
            node.Release576(!rollback);
            if (rollback)
                ReleaseRegisteredHeader526(node.Block);
        }
    }

    private void RetireMagicReferences576()
        => PrepareMagicSweep576(static _ => true, false, null)?.Commit576();
}
