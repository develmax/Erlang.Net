namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private nint writableOffHeap;

    internal bool PinWritableBinary(OrdinaryBinaryBacking owner, WritableBinaryBinding binding)
    {
        lock (Gate)
        {
            if (!ReferenceEquals(owner.WritableBinding, binding)) return false;
            var node = RequireNativeOffheapHeader576(binding.BinRef.Block, NativeOffheapKind576.BinaryReference);
            if (!ReferenceEquals(node, binding.BinRef) || !ReferenceEquals(node.OrdinaryBinary, owner))
                throw new InvalidOperationException(NativeWritableBinaryLayout.Invalid);
            var state = owner.WritableState;
            if ((state.Flags & NativeWritableBinaryLayout.Writable) == 0) return true;
            int used = checked((int)state.Apparent);
            int size = state.Capacity - used >= NativeWritableBinaryLayout.PinSpareBytes ? used : state.Capacity;
            using OrdinaryBinaryMutation mutation = owner.PrepareMutation(size, 0, state.Apparent);
            owner.CommitMutation(mutation, binding);

            return true;
        }
    }

    internal BitString? AppendWritableBinary(
        OwnedTermInvocation506 invocation,
        BitString source,
        byte[] complete,
        int totalBits,
        CancellationToken cancellation
    )
    {
        lock (Gate)
        {
            cancellation.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(closing, this);
            if (source.OwnedStorage577 is not ProcessTermSequence506 original || !ReferenceEquals(original.Owner, this)) return null;
            ResourceBinaryDescriptor577 descriptor = RequireResourceBitsDescriptor577(original);
            OrdinaryBinaryBacking? owner = descriptor.BinRef.OrdinaryBinary;
            if (owner?.WritableBinding is not { } binding || !ReferenceEquals(binding.Heap, this)
                || !ReferenceEquals(binding.BinRef, descriptor.BinRef) || !owner.IsWritable) return null;
            int sourceOffset = original.AllocationOffset526;
            nuint oldBase = original.Slab.Cells.ReadNumeric556(sourceOffset + ResourceBinary577Names.BaseWord);
            if ((oldBase & ResourceBinary577Names.FlagMask) != NativeWritableBinaryLayout.CurrentWriter) return null;
            ResourceBitsRead577 read = ReadResourceBits577(original);
            if (read.BitOffset != 0 || totalBits < read.BitLength || complete.Length != checked((totalBits + 7) / 8))
                throw new InvalidOperationException(NativeWritableBinaryLayout.Invalid);
            if (totalBits == read.BitLength) return source;
            using ProcessTermFrame506 staging = Enter();
            ProcessTermConstruction514 custody = staging.BeginConstruction514(source);
            custody.PrepareResourceBitsTracking577();
            resourceBits577.EnsureCapacity(checked(resourceBits577.Count + 1));
            var state = owner.WritableState;
            int capacity = state.Capacity;
            if (complete.Length > capacity)
            {
                double desired = complete.Length <= BinaryBackingParameters.LargeGrowthThreshold
                    ? complete.Length * 2d : complete.Length * BinaryBackingParameters.LargeGrowthFactor;
                if (desired > Array.MaxLength) throw new ErlangException(ErlangErrorReasons.SystemLimit);
                capacity = (int)desired;
            }
            using OrdinaryBinaryMutation mutation = owner.PrepareMutation(
                capacity,
                NativeWritableBinaryLayout.WritableActive,
                (nuint)complete.Length,
                complete
            );
            nuint[] oldFlags = [oldBase & ~NativeWritableBinaryLayout.Mutable];
            nuint[] nextFlags = [mutation.PointerCell[0] + (nuint)System.Runtime.InteropServices.Marshal.SizeOf<OrdinaryBinaryHeader>() | NativeWritableBinaryLayout.CurrentWriter];
            nuint[] nextEnd = [(nuint)totalBits];
            nuint origin = (nuint)descriptor.BinRef.Address | ResourceBinary577Names.BoxedTag;
            ProcessTermSequence506? block = null;
            ResourceBinaryDescriptor577? created = null;
            try
            {
                // Start with the old valid range so all fallible root publication
                // can finish BEFORE touching writer flags, payload or pointers.
                block = AllocateEncoded526(
                    [],
                    [ResourceBinary577Names.Header],
                    [(nuint)descriptor.Data | NativeWritableBinaryLayout.CurrentWriter, 0, (nuint)read.BitLength, origin]
                );
                created = new(
                    this,
                    block,
                    descriptor.BinRef,
                    descriptor.Backing,
                    descriptor.Data,
                    descriptor.Bytes
                );
                resourceBits577.Add(block.Identity, created);
                custody.TrackResourceBits577(created);
                var result = new BitString(descriptor.Backing, block);
                custody.Publish(result);
                invocation.HoldGuestResult507(result);
                cancellation.ThrowIfCancellationRequested();
                owner.CommitMutation(mutation, binding);
                original.Slab.Cells.WriteNumeric556(oldFlags, sourceOffset + ResourceBinary577Names.BaseWord);
                block.Slab.Cells.WriteNumeric556(nextFlags, block.AllocationOffset526 + ResourceBinary577Names.BaseWord);
                block.Slab.Cells.WriteNumeric556(nextEnd, block.AllocationOffset526 + ResourceBinary577Names.EndWord);

                return result;
            }
            catch
            {
                // A successfully published internal result remains a real root
                // until invocation unwind; never release a published descriptor.
                if (block is not null && created?.Published != true)
                {
                    resourceBits577.Remove(block.Identity);
                    if (blocks.TryGetValue(block.Identity, out var live) && ReferenceEquals(live, block)) ReleaseRegisteredHeader526(block);
                }
                throw;
            }
        }
    }
}
