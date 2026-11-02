namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal bool CanOwnOrdinaryBinary(BitString source)
        => !runtime.IsSharingLiteral425(source)
            && (source.ResourceBacking577.NativeStorage524 is OrdinaryBinaryBacking
                || (source.ResourceBacking577.CanCaptureImmutableOrdinary || source.ResourceBacking577.HasWritableOrigin) && source.HasReferenceStorage425);

    internal BitString CreateOrdinaryBinary(
        BitString source,
        ProcessTermConstruction514 construction,
        PreparedOrdinaryBinary prepared,
        bool preserveOrigin
    )
    {
        if (!Monitor.IsEntered(Gate) || !construction.Contains(source) || !CanOwnOrdinaryBinary(source)
            || !ReferenceEquals(prepared.Owner.Runtime, runtime))
            throw new InvalidOperationException(OrdinaryBinaryNames.Prepared);
        if (LiteralDomain800 is not null && (!CanOwnModuleImmutableOrdinary805(source)
            || prepared.Owner.IsWritable || prepared.Owner.WritableBinding is not null || prepared.CurrentWriter))
            throw new InvalidOperationException(ModuleBinary805Names.Invalid);
        ResourceBitsRead577 read = source.CaptureResourceBits577();
        NativeOffheapHeader576? binRef = null;
        bool created = false;
        if (preserveOrigin && source.ResourceSliceOrigin577 is { } origin
            && construction.Contains(origin) && Storage(origin) is ProcessTermSequence506 originStorage
            && ReferenceEquals(originStorage.Owner, this) && TryGetResourceBitsDescriptor577(origin, out var original)
            && ReferenceEquals(original!.BinRef.OrdinaryBinary, prepared.Owner))
            binRef = original.BinRef;
        WritableBinaryBinding? binding = prepared.Owner.WritableBinding;
        if (binding is not null && ReferenceEquals(binding.Heap, this))
        {
            _ = RequireNativeOffheapHeader576(binding.BinRef.Block, NativeOffheapKind576.BinaryReference);
            binRef = binding.BinRef;
        }
        construction.PrepareResourceBitsTracking577();
        resourceBits577.EnsureCapacity(checked(resourceBits577.Count + 1));
        ProcessTermSequence506? block = null;
        try
        {
            if (binRef is null)
            {
                binRef = AllocateOrdinaryBinaryReference(source, prepared.Owner, construction);
                created = true;
            }
            INativeBinaryStorage524 storage = prepared.Owner;
            nint data = storage.Pointer524;
            nuint start = checked((nuint)read.BitOffset);
            nuint end = checked(start + (nuint)read.BitLength);
            if (end > checked((nuint)storage.DataLength524 * BitStringLayout.BitsPerByte))
                throw new InvalidOperationException(OrdinaryBinaryNames.Invalid);
            nuint originWord = (nuint)ReadNativeOffheapAddress576(binRef.Block, NativeOffheapKind576.BinaryReference)
                | ResourceBinary577Names.BoxedTag;
            nuint flags = prepared.Owner.IsWritable ? (prepared.CurrentWriter ? NativeWritableBinaryLayout.CurrentWriter : NativeWritableBinaryLayout.Volatile) : 0;
            block = AllocateEncoded526([], [ResourceBinary577Names.Header], [(nuint)data | flags, start, end, originWord]);
            var descriptor = new ResourceBinaryDescriptor577(
                this,
                block,
                binRef,
                prepared.Backing,
                data,
                storage.DataLength524
            );
            resourceBits577.Add(block.Identity, descriptor);
            construction.TrackResourceBits577(descriptor);
            var value = new BitString(prepared.Backing, block);
            construction.Publish(value);

            return value;
        }
        catch
        {
            if (block is not null)
            {
                resourceBits577.Remove(block.Identity);
                if (blocks.TryGetValue(block.Identity, out var live) && ReferenceEquals(live, block))
                    ReleaseRegisteredHeader526(block);
            }
            if (created && binRef is not null) RollbackNativeOffheapHeader576(binRef);
            throw;
        }
    }

    private NativeOffheapHeader576 AllocateOrdinaryBinaryReference(Term source, OrdinaryBinaryBacking owner,
        ProcessTermConstruction514 construction)
    {
        if (!Monitor.IsEntered(Gate) || closing || !frames.Contains(construction.Frame517)
            || construction.Frame517.IsDisposed || !construction.Contains(source) || !ReferenceEquals(owner.Runtime, runtime))
            throw new InvalidOperationException(OrdinaryBinaryNames.Prepared);
        RequireReferencePlatform562();
        construction.PrepareMagicTracking576();
        magicReferences576.EnsureCapacity(checked(magicReferences576.Count + 1));
        magicAddresses576.EnsureCapacity(checked(magicAddresses576.Count + 1));
        OrdinaryBinaryLease? lease = owner.Retain();
        ProcessTermSequence506? block = null;
        NativeOffheapHeader576? node = null;
        bool writableChain = owner.IsWritable;
        nint previous = writableChain ? writableOffHeap : magicOffHeap576;
        bool byId = false, byAddress = false, linked = false;
        try
        {
            nint binary = owner.BinaryPointer;
            block = AllocateEncoded526([], [NativeOffheap576Names.BinaryHeader], [(nuint)binary, (nuint)previous]);
            nint address = CaptureRawAddress571(block, 0, NativeOffheap576Names.Words).Address571;
            node = new NativeOffheapHeader576(
                this,
                block,
                owner,
                address,
                lease
            );
            magicReferences576.Add(block.Identity, node);
            byId = true;
            magicAddresses576.Add(address, node);
            byAddress = true;
            construction.TrackMagic576(node);
            if (writableChain)
            {
                owner.BindWritable(new(this, node));
                writableOffHeap = address;
            }
            else magicOffHeap576 = address;
            linked = true;
            lease = null;

            return node;
        }
        catch
        {
            if (linked)
            {
                if (writableChain) writableOffHeap = previous;
                else magicOffHeap576 = previous;
            }
            if (byAddress) magicAddresses576.Remove(node!.Address);
            if (byId) magicReferences576.Remove(block!.Identity);
            if (block is not null) ReleaseRegisteredHeader526(block);
            if (node is not null) node.Release576(false); else lease?.Dispose();
            throw;
        }
    }
}
