namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // Inspect raw flags BEFORE the ordinary reader: a volatile reader may
    // repair a SubBits base, which is not an allowed reservation side effect.
    internal bool TryCaptureReadonlyTraceBinary817(BitString source, out ReadonlyTraceBinary817? result)
    {
        result = null;
        if (!Monitor.IsEntered(Gate)) throw new InvalidOperationException(CellStorage526Names.GateRequired);
        if (source.OwnedStorage577 is not ProcessTermSequence506 block || !IsOwnedLocal(source)
            || source.ResourceSliceOrigin577 is not null || LiteralDomain800 is not null) return false;
        ResourceBinaryDescriptor577 descriptor = RequireResourceBitsDescriptor577(block);
        nuint flags = block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + ResourceBinary577Names.BaseWord)
            & ResourceBinary577Names.FlagMask;
        if (flags != 0 || descriptor.BinRef.OrdinaryBinary is not { } binary
            || !ReferenceEquals(binary.Runtime, runtime) || binary.IsWritable || binary.WritableBinding is not null) return false;
        _ = RequireNativeOffheapHeader576(descriptor.BinRef.Block, NativeOffheapKind576.BinaryReference);
        if (descriptor.Bytes > int.MaxValue / BitStringLayout.BitsPerByte) return false;
        ResourceBitsRead577 read = ReadResourceBits577(block);
        if (read.BitOffset != 0 || read.BitLength != checked(descriptor.Bytes * BitStringLayout.BitsPerByte)) return false;
        result = new(
            source,
            binary,
            descriptor.Backing,
            read
        );

        return true;
    }

    // The output heap is a private unpublished fragment. The source capability
    // still owns the existing Entry operation. No independent native share is
    // claimed until selected-event RetainReadonlyReserved817 succeeds.
    internal BitString CreateReservedReadonlyTraceBinary817(
        ReadonlyTraceArena817 arena,
        BitString value,
        ProcessTermConstruction514 construction
    )
    {
        if (!Monitor.IsEntered(Gate) || closing || !ReferenceEquals(arena.Heap, this)
            || !ReferenceEquals(arena.Frame, construction.Frame517) || !frames.Contains(construction.Frame517)
            || construction.Frame517.IsDisposed || !ReferenceEquals(arena.Source.Runtime, runtime))
            throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        ReadonlyTraceSource817 source = arena.Source;
        List<OrdinaryBinaryLease> reserved = arena.Leases;
        source.RequireValidated();
        ReadonlyTraceBinary817 input = source.Binary(value);
        OrdinaryBinaryBacking binary = input.Owner;
        if (binary.IsWritable || binary.WritableBinding is not null)
            throw new InvalidOperationException(ReadonlyTrace817Names.Unsupported);
        RequireReferencePlatform562();
        construction.PrepareMagicTracking576();
        construction.PrepareResourceBitsTracking577();
        magicReferences576.EnsureCapacity(checked(magicReferences576.Count + 1));
        magicAddresses576.EnsureCapacity(checked(magicAddresses576.Count + 1));
        resourceBits577.EnsureCapacity(checked(resourceBits577.Count + 1));
        reserved.EnsureCapacity(checked(reserved.Count + 1));
        OrdinaryBinaryLease? lease = binary.ReserveLease();
        ProcessTermSequence506? binBlock = null, bitsBlock = null;
        NativeOffheapHeader576? node = null;
        bool byId = false, byAddress = false, linked = false;
        nint previous = magicOffHeap576;
        try
        {
            nint binaryPointer = binary.BinaryPointer;
            binBlock = AllocateEncoded526([], [NativeOffheap576Names.BinaryHeader], [(nuint)binaryPointer, (nuint)previous]);
            nint address = CaptureRawAddress571(binBlock, 0, NativeOffheap576Names.Words).Address571;
            node = new(
                this,
                binBlock,
                binary,
                address,
                lease
            );
            magicReferences576.Add(binBlock.Identity, node);
            byId = true;
            magicAddresses576.Add(address, node);
            byAddress = true;
            construction.TrackMagic576(node);
            magicOffHeap576 = address;
            linked = true;
            nint data = ((INativeBinaryStorage524)binary).Pointer524;
            nuint start = checked((nuint)input.Bits.BitOffset);
            nuint end = checked(start + (nuint)input.Bits.BitLength);
            bitsBlock = AllocateEncoded526([], [ResourceBinary577Names.Header],
                [(nuint)data, start, end, (nuint)address | ResourceBinary577Names.BoxedTag]);
            var descriptor = new ResourceBinaryDescriptor577(
                this,
                bitsBlock,
                node,
                input.Backing,
                data,
                ((INativeBinaryStorage524)binary).DataLength524
            );
            resourceBits577.Add(bitsBlock.Identity, descriptor);
            construction.TrackResourceBits577(descriptor);
            var result = new BitString(input.Backing, bitsBlock);
            construction.Publish(result);
            reserved.Add(lease);
            lease = null;

            return result;
        }
        catch
        {
            if (bitsBlock is not null)
            {
                resourceBits577.Remove(bitsBlock.Identity);
                if (blocks.TryGetValue(bitsBlock.Identity, out var live) && ReferenceEquals(live, bitsBlock))
                    ReleaseRegisteredHeader526(bitsBlock);
            }
            if (linked) magicOffHeap576 = previous;
            if (byAddress) magicAddresses576.Remove(node!.Address);
            if (byId) magicReferences576.Remove(binBlock!.Identity);
            if (binBlock is not null) ReleaseRegisteredHeader526(binBlock);
            if (node is not null) node.Release576(false);
            lease?.Dispose();
            throw;
        }
    }
}
