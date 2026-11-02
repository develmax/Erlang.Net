namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal bool CanOwnModuleImmutableOrdinary805(BitString source)
        => LiteralDomain800 is { IsSealed800: false }
            && source.ResourceBacking577.NativeStorage524 is null
            && source.ResourceBacking577.CanCaptureImmutableOrdinary
            && !source.ResourceBacking577.HasWritableOrigin
            && source.HasReferenceStorage425
            && source.BitLength > HeapBitStringLayout.MaximumBytes * HeapBitStringLayout.BitsPerByte;

    private ResourceBinaryDescriptor577 RequireImmutableModuleDescriptor805(ProcessTermSequence506 block)
    {
        if (!ReferenceEquals(block.Owner, this)) throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        if (!blocks.TryGetValue(block.Identity, out ProcessTermSequence506? active) || !ReferenceEquals(active, block)
            || !resourceBits577.TryGetValue(block.Identity, out ResourceBinaryDescriptor577? descriptor)
            || !ReferenceEquals(descriptor.Owner, this) || !ReferenceEquals(descriptor.Block, block))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        if (block.Count != 0 || block.HeaderWords526 != ResourceBinary577Names.HeaderWords
            || block.RawPayloadWords556 != ResourceBinary577Names.DataWords
            || block.Slab.Cells.ReadHeader526(block.AllocationOffset526) != ResourceBinary577Names.Header)
            throw new InvalidOperationException(ModuleBinary805Names.Invalid);

        return descriptor;
    }

    private ResourceBitsRead577 ReadImmutableModuleBinary805(ProcessTermSequence506 block, ModuleLiteralDomain800 domain)
    {
        if (!ReferenceEquals(LiteralDomain800, domain) || !domain.IsSealed800)
            throw new InvalidOperationException(ModuleBinary805Names.Invalid);
        using ModuleLiteralRead800 read = domain.EnterRead800(block);
        ResourceBinaryDescriptor577 descriptor = RequireImmutableModuleDescriptor805(block);
        NativeOffheapHeader576 binRef = descriptor.BinRef;
        ProcessTermSequence506 reference = binRef.Block;
        using ModuleLiteralRead800 referenceRead = domain.EnterRead800(reference);
        OrdinaryBinaryBacking ordinary = binRef.OrdinaryBinary ?? throw new InvalidOperationException(ModuleBinary805Names.Invalid);
        if (!ReferenceEquals(binRef.Owner, this) || binRef.Kind != NativeOffheapKind576.BinaryReference
            || !ReferenceEquals(ordinary.Runtime, runtime) || ordinary.IsWritable || ordinary.WritableBinding is not null
            || !ReferenceEquals(descriptor.Backing.NativeStorage524, ordinary)
            || !magicReferences576.TryGetValue(reference.Identity, out NativeOffheapHeader576? registered)
            || !ReferenceEquals(registered, binRef) || !ReferenceEquals(reference.Owner, this)
            || !blocks.TryGetValue(reference.Identity, out ProcessTermSequence506? live) || !ReferenceEquals(live, reference)
            || reference.Count != 0 || reference.HeaderWords526 != NativeOffheap576Names.HeaderWords
            || reference.RawPayloadWords556 != NativeOffheap576Names.DataWords
            || reference.Slab.Cells.ReadHeader526(reference.AllocationOffset526) != NativeOffheap576Names.BinaryHeader)
            throw new InvalidOperationException(ModuleBinary805Names.Invalid);
        // This exact immutable reader admits both actual blocks. The existing
        // raw-cell checker still rejects Term/GCHandle cells; no generic heap
        // raw-address gate or process ownership rule is weakened.
        nint address = reference.Slab.Cells.RawAddress571(reference.AllocationOffset526, NativeOffheap576Names.Words);
        nint binary = ordinary.BinaryPointer;
        nint next = (nint)reference.Slab.Cells.ReadNumeric556(reference.AllocationOffset526 + NativeOffheap576Names.NextWord);
        if (address != binRef.Address || binary == 0 || ((nuint)binary & (nuint)(IntPtr.Size - 1)) != 0
            || reference.Slab.Cells.ReadNumeric556(reference.AllocationOffset526 + NativeOffheap576Names.BinaryWord) != (nuint)binary
            || next == address || next != 0 && !magicAddresses576.ContainsKey(next))
            throw new InvalidOperationException(ModuleBinary805Names.Invalid);
        int offset = block.AllocationOffset526;
        nuint origin = block.Slab.Cells.ReadNumeric556(offset + ResourceBinary577Names.OriginWord);
        nuint data = block.Slab.Cells.ReadNumeric556(offset + ResourceBinary577Names.BaseWord);
        nuint start = block.Slab.Cells.ReadNumeric556(offset + ResourceBinary577Names.StartWord);
        nuint end = block.Slab.Cells.ReadNumeric556(offset + ResourceBinary577Names.EndWord);
        INativeBinaryStorage524 bytes = ordinary;
        if (origin != ((nuint)address | ResourceBinary577Names.BoxedTag)
            || (data & ResourceBinary577Names.FlagMask) != 0 || data != (nuint)bytes.Pointer524
            || end < start || end - start > int.MaxValue)
            throw new InvalidOperationException(ModuleBinary805Names.Invalid);
        Int128 bitOffset = (Int128)(UInt128)start;
        Int128 size = (Int128)(UInt128)(end - start);
        if (bitOffset < 0 || bitOffset > (Int128)bytes.DataLength524 * BitStringLayout.BitsPerByte - size)
            throw new InvalidOperationException(ModuleBinary805Names.Invalid);

        return new(checked((long)bitOffset), checked((int)size));
    }
}
