namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private readonly Dictionary<long, ResourceBinaryDescriptor577> resourceBits577 = new();

    internal bool IsNativeResourceBinary577(Term value)
        => value is BitString bits
            && bits.ResourceBacking577.NativeStorage524?.ResourceIdentity524 is NativeResourcePayload524;

    internal BitString CreateResourceBits577(BitString source, ProcessTermConstruction514 construction, bool preserveSliceOrigin = true)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        if (closing || !frames.Contains(construction.Frame517) || construction.Frame517.IsDisposed
            || !construction.Contains(source))
            throw new InvalidOperationException(ResourceBinary577Names.Custody);
        if (Storage(source) is { } existing && !ReferenceEquals(existing.Owner, this))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        BinaryBacking backing = source.ResourceBacking577;
        INativeBinaryStorage524 native = backing.NativeStorage524
            ?? throw new InvalidOperationException(ResourceBinary577Names.Invalid);
        NativeResourcePayload524 payload = native.ResourceIdentity524 as NativeResourcePayload524
            ?? throw new InvalidOperationException(ResourceBinary577Names.Invalid);
        if (!ReferenceEquals(payload.Runtime, runtime))
            throw new NotSupportedException(NativeResource524Names.Foreign);
        ResourceBitsRead577 read = source.CaptureResourceBits577();
        nint data = native.Pointer524;
        int bytes = native.DataLength524;
        if (read.BitOffset < 0 || read.BitLength < 0 || read.BitOffset > checked((long)bytes * BitStringLayout.BitsPerByte) - read.BitLength)
            throw new InvalidOperationException(ResourceBinary577Names.Invalid);
        NativeOffheapHeader576? binRef = null;
        bool createdBinRef = false;
        if (preserveSliceOrigin && source.ResourceSliceOrigin577 is { } origin)
        {
            if (!construction.Contains(origin) || !TryGetResourceBitsDescriptor577(origin, out var original)
                || !ReferenceEquals(original!.Backing, backing))
                throw new InvalidOperationException(ResourceBinary577Names.Custody);
            binRef = original.BinRef;
        }
        construction.PrepareResourceBitsTracking577();
        resourceBits577.EnsureCapacity(checked(resourceBits577.Count + 1));
        ProcessTermSequence506? block = null;
        ResourceBinaryDescriptor577? descriptor = null;
        try
        {
            if (binRef is null)
            {
                binRef = AllocateNativeOffheapHeader576(
                    source,
                    payload,
                    NativeOffheapKind576.BinaryReference,
                    construction
                );
                createdBinRef = true;
            }
            _ = RequireNativeOffheapHeader576(binRef.Block, NativeOffheapKind576.BinaryReference);
            nuint rawData = (nuint)data;
            nuint adjustment = rawData & ResourceBinary577Names.FlagMask;
            nuint start = checked((nuint)read.BitOffset + adjustment * BitStringLayout.BitsPerByte);
            nuint end = checked(start + (nuint)read.BitLength);
            nuint originWord = (nuint)ReadNativeOffheapAddress576(binRef.Block, NativeOffheapKind576.BinaryReference)
                | ResourceBinary577Names.BoxedTag;
            block = AllocateEncoded526([], [ResourceBinary577Names.Header], [rawData - adjustment, start, end, originWord]);
            descriptor = new(
                this,
                block,
                binRef,
                backing,
                data,
                bytes
            );
            resourceBits577.Add(block.Identity, descriptor);
            construction.TrackResourceBits577(descriptor);
            var result = new BitString(backing, block);
            construction.Publish(result);

            return result;
        }
        catch
        {
            if (block is not null)
            {
                resourceBits577.Remove(block.Identity);
                if (blocks.TryGetValue(block.Identity, out var active) && ReferenceEquals(active, block))
                    ReleaseRegisteredHeader526(block);
            }
            if (createdBinRef && binRef is not null)
                RollbackNativeOffheapHeader576(binRef);
            throw;
        }
    }

    internal ResourceBitsRead577 ReadResourceBits577(ProcessTermSequence506 block)
    {
        if (LiteralDomain800 is { IsSealed800: true } domain) return ReadImmutableModuleBinary805(block, domain);
        lock (Gate)
        {
            ResourceBinaryDescriptor577 descriptor = RequireResourceBitsDescriptor577(block);
            NativeOffheapHeader576 binRef = RequireNativeOffheapHeader576(descriptor.BinRef.Block, NativeOffheapKind576.BinaryReference);
            int offset = block.AllocationOffset526;
            nuint origin = block.Slab.Cells.ReadNumeric556(offset + ResourceBinary577Names.OriginWord);
            if (origin != ((nuint)ReadNativeOffheapAddress576(binRef.Block, NativeOffheapKind576.BinaryReference) | ResourceBinary577Names.BoxedTag))
                throw new InvalidOperationException(ResourceBinary577Names.Invalid);
            nuint baseFlags = block.Slab.Cells.ReadNumeric556(offset + ResourceBinary577Names.BaseWord);
            nuint start = block.Slab.Cells.ReadNumeric556(offset + ResourceBinary577Names.StartWord);
            nuint end = block.Slab.Cells.ReadNumeric556(offset + ResourceBinary577Names.EndWord);
            nuint flags = baseFlags & ResourceBinary577Names.FlagMask;
            if (end < start || end - start > int.MaxValue || flags == NativeWritableBinaryLayout.Mutable
                || flags != 0 && binRef.OrdinaryBinary is null)
                throw new InvalidOperationException(ResourceBinary577Names.Invalid);
            if ((flags & NativeWritableBinaryLayout.Volatile) != 0)
            {
                // Stored base can be stale after REAL relocation. Follow the
                // actual BinRef native pointer, as ERTS_GET_BITSTRING_REF does.
                baseFlags = (nuint)descriptor.Data;
                if (!binRef.OrdinaryBinary!.IsWritable)
                    block.Slab.Cells.WriteNumeric556([baseFlags], offset + ResourceBinary577Names.BaseWord);
            }
            // Align-down compensation is source-defined. Rebase to the genuine
            // leased range to avoid ever reading padding/outside native memory.
            Int128 bitOffset = ((Int128)(UInt128)baseFlags - (Int128)(nuint)descriptor.Data) * BitStringLayout.BitsPerByte + (Int128)(UInt128)start;
            Int128 size = (Int128)(UInt128)(end - start);
            if (bitOffset < 0 || bitOffset > (Int128)descriptor.Bytes * BitStringLayout.BitsPerByte - size)
                throw new InvalidOperationException(ResourceBinary577Names.Invalid);
            if (binRef.OrdinaryBinary is { } ordinary)
            {
                if (ordinary.BinaryPointer != binRef.Binary) throw new InvalidOperationException(ResourceBinary577Names.Invalid);
            }
            else binRef.Payload.ValidateRead524();

            return new(checked((long)bitOffset), checked((int)size));
        }
    }

    private ResourceBinaryDescriptor577 RequireResourceBitsDescriptor577(ProcessTermSequence506 block)
    {
        if (LiteralDomain800 is { IsSealed800: true } domain)
        {
            using ModuleLiteralRead800 read = domain.EnterRead800(block);

            return RequireImmutableModuleDescriptor805(block);
        }
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        if (!ReferenceEquals(block.Owner, this))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        if (!blocks.TryGetValue(block.Identity, out var active) || !ReferenceEquals(active, block)
            || !resourceBits577.TryGetValue(block.Identity, out var descriptor) || !ReferenceEquals(descriptor.Block, block))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        if (block.Count != 0 || block.HeaderWords526 != ResourceBinary577Names.HeaderWords
            || block.RawPayloadWords556 != ResourceBinary577Names.DataWords
            || block.Slab.Cells.ReadHeader526(block.AllocationOffset526) != ResourceBinary577Names.Header)
            throw new InvalidOperationException(ResourceBinary577Names.Invalid);

        return descriptor;
    }

    internal bool TryGetResourceBitsDescriptor577(Term value, out ResourceBinaryDescriptor577? descriptor)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        descriptor = null;
        if (value is not BitString { OwnedStorage577: ProcessTermSequence506 block })
            return false;
        _ = ReadResourceBits577(block);
        descriptor = RequireResourceBitsDescriptor577(block);

        return true;
    }

    internal bool TryGetResourceBinaryNode577(Term value, out NativeOffheapHeader576? node)
    {
        node = null;
        if (!TryGetResourceBitsDescriptor577(value, out var descriptor))
            return false;
        node = descriptor!.BinRef;

        return true;
    }

    private void MarkResourceBinaryNodes577(HashSet<Term> seen, bool major, HashSet<long> marked)
    {
        // Existing mark pass marks seen SubBits through Storage(value). Old raw
        // SubBits retained by minor still need their shared BinRef kept alive.
        foreach (BitString raw in seen.OfType<BitString>())
            if (raw.OwnedStorage577 is null && raw.ResourceBacking577.NativeStorage524 is OrdinaryBinaryBacking owner
                && owner.WritableBinding is { } binding && ReferenceEquals(binding.Heap, this)
                && blocks.TryGetValue(binding.BinRef.Block.Identity, out var live) && ReferenceEquals(live, binding.BinRef.Block))
                marked.Add(binding.BinRef.Block.Identity);
        foreach (ResourceBinaryDescriptor577 descriptor in resourceBits577.Values)
        {
            bool retained = marked.Contains(descriptor.Block.Identity)
                || !major && oldBlocks508.Contains(descriptor.Block.Identity);
            if (!retained)
                continue;
            _ = ReadResourceBits577(descriptor.Block);
            marked.Add(descriptor.BinRef.Block.Identity);
        }
    }

    private void ForgetResourceBinaryBlocks577(long[] dead)
    {
        foreach (long identity in dead)
            resourceBits577.Remove(identity);
    }

    private void RetireResourceBinaryDescriptors577() => resourceBits577.Clear();

    internal void RollbackResourceBinaryBlocks577(IReadOnlyList<ResourceBinaryDescriptor577> created)
    {
        lock (Gate)
        {
            foreach (ResourceBinaryDescriptor577 descriptor in created)
            {
                if (descriptor.Published || !resourceBits577.Remove(descriptor.Block.Identity))
                    continue;
                if (blocks.TryGetValue(descriptor.Block.Identity, out var active) && ReferenceEquals(active, descriptor.Block))
                    ReleaseRegisteredHeader526(descriptor.Block);
            }
        }
    }
}
