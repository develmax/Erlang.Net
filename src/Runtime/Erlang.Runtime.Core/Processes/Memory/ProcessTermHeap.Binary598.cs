namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private ResourceReferenceTerm CreateBinaryMagic598(ResourceReferenceTerm source, ProcessTermConstruction514 construction)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        if (closing || !frames.Contains(construction.Frame517) || construction.Frame517.IsDisposed
            || !construction.Contains(source))
            throw new InvalidOperationException(MagicReference576Names.MissingCustody);
        BinaryProgramBacking598 backing = BinaryProgramRoots598.Direct598(source, runtime)
            ?? throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        if (!ReferenceEquals(source.Entry.Owner, runtime.ReferenceResources))
            throw new NotSupportedException(NativeResource524Names.Foreign);
        RequireReferencePlatform562();
        construction.PrepareMagicTracking576();
        magicReferences576.EnsureCapacity(checked(magicReferences576.Count + 1));
        magicAddresses576.EnsureCapacity(checked(magicAddresses576.Count + 1));
        BinaryProgramLease598? lease = backing.Retain598(BinaryProgramLeaseKind598.Guest);
        ProcessTermSequence506? block = null;
        NativeOffheapHeader576? node = null;
        nint previous = magicOffHeap576;
        bool byIdentity = false, byAddress = false, linked = false;
        try
        {
            nint binary = backing.Binary598(lease);
            IReadOnlyList<uint> numbers = backing.Numbers598(lease);
            if (numbers.Count != OrdinaryReference562Names.NumberCount
                || (numbers[1] & ReferenceNumberLayout.MagicMarker) == 0
                || binary == 0 || ((nuint)binary & (nuint)(IntPtr.Size - 1)) != 0)
                throw new InvalidOperationException(MagicReference576Names.Invalid);
            block = AllocateEncoded526([], [NativeOffheap576Names.Header(NativeOffheapKind576.MagicReference)], [(nuint)binary, (nuint)previous]);
            nint address = CaptureRawAddress571(block, 0, MagicReference576Names.Words).Address571;
            node = new(
                this,
                block,
                backing,
                binary,
                address,
                lease
            );
            magicReferences576.Add(block.Identity, node);
            byIdentity = true;
            magicAddresses576.Add(address, node);
            byAddress = true;
            construction.TrackMagic576(node);
            magicOffHeap576 = address;
            linked = true;
            lease = null;
            var result = new ResourceReferenceTerm(source.Entry, block);
            node.MagicTerm = result;
            construction.Publish(result);

            return result;
        }
        catch
        {
            if (node?.Published == true)
                throw;
            if (linked)
                magicOffHeap576 = previous;
            if (byAddress)
                magicAddresses576.Remove(node!.Address);
            if (byIdentity)
                magicReferences576.Remove(block!.Identity);
            if (block is not null)
                ReleaseRegisteredHeader526(block);
            if (node is not null)
                node.Release576(false);
            else
                lease?.Dispose();
            throw;
        }
    }

    internal BinaryProgramBacking598 ReadBinaryMagic598(ResourceReferenceTerm reference, BinaryProgramBacking598 expected)
    {
        lock (Gate)
        {
            if (reference.OwnedStorage562 is not ProcessTermSequence506 block)
                throw new InvalidOperationException(MagicReference576Names.Invalid);
            NativeOffheapHeader576 node = RequireMagicReference576(block);
            if (!ReferenceEquals(node.MagicTerm, reference) || !ReferenceEquals(node.Binary598, expected))
                throw new InvalidOperationException(MagicReference576Names.Invalid);
            _ = node.ReadNumbers586();

            return expected;
        }
    }
}
