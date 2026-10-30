using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe partial class OrdinaryBinaryBacking
{
    private long mutationGeneration;
    private WritableBinaryBinding? writableBinding;
    internal WritableBinaryBinding? WritableBinding
    {
        get
        {
            lock (gate) return writableBinding;
        }
    }
    internal bool IsWritable
    {
        get
        {
            lock (gate) return (((OrdinaryBinaryHeader*)Require())->Flags & NativeWritableBinaryLayout.Writable) != 0;
        }
    }
    internal (nuint Flags, nuint Apparent, int Capacity) WritableState
    {
        get
        {
            lock (gate)
            {
                var header = (OrdinaryBinaryHeader*)Require();

                return (header->Flags, header->ApparentSize, capacity);
            }
        }
    }

    public byte ReadWritableByte(int index)
    {
        lock (gate)
        {
            nint current = Require();
            if ((uint)index >= (uint)capacity) throw new ArgumentOutOfRangeException(nameof(index));

            return *((byte*)current + sizeof(OrdinaryBinaryHeader) + index);
        }
    }

    internal static OrdinaryBinaryBacking CaptureWritable(ProcessRuntime runtime, WritableBinarySnapshot snapshot, OrdinaryBinaryAllocationTicket ticket)
    {
        nint pointer = 0;
        try
        {
            if (IntPtr.Size != sizeof(ulong) || sizeof(OrdinaryBinaryHeader) != 32
                || snapshot.ApparentBytes < 0 || snapshot.ApparentBytes > snapshot.Bytes.Length)
                throw new InvalidOperationException(NativeWritableBinaryLayout.Invalid);
            pointer = (nint)NativeMemory.AllocZeroed(checked((nuint)sizeof(OrdinaryBinaryHeader) + (nuint)snapshot.Bytes.Length));
            if (pointer == 0) throw new OutOfMemoryException();
            var header = (OrdinaryBinaryHeader*)pointer;
            header->References = 1;
            header->OriginalSize = snapshot.Bytes.Length;
            header->ApparentSize = (nuint)snapshot.ApparentBytes;
            header->Flags = snapshot.CurrentWriter ? NativeWritableBinaryLayout.WritableActive : 0;
            snapshot.Bytes.AsSpan().CopyTo(new Span<byte>((byte*)pointer + sizeof(OrdinaryBinaryHeader), snapshot.Bytes.Length));

            return new(
                runtime,
                snapshot.Bytes.Length,
                pointer,
                ticket
            );
        }
        catch
        {
            NativeMemory.Free((void*)pointer);
            ticket.Complete();
            throw;
        }
    }

    internal void BindWritable(WritableBinaryBinding binding)
    {
        lock (gate)
        {
            if ((((OrdinaryBinaryHeader*)Require())->Flags & NativeWritableBinaryLayout.Writable) == 0) return;
            if (writableBinding is not null) throw new InvalidOperationException(NativeWritableBinaryLayout.Invalid);
            writableBinding = binding;
        }
    }

    internal void DetachWritable(NativeOffheapHeader576 binRef)
    {
        lock (gate)
        {
            if (writableBinding is not { } binding || !ReferenceEquals(binding.BinRef, binRef)) return;
            long next = checked(mutationGeneration + 1);
            writableBinding = null;
            ((OrdinaryBinaryHeader*)Require())->Flags = 0;
            mutationGeneration = next;
        }
    }

    internal OrdinaryBinaryMutation PrepareMutation(
        int nextCapacity,
        nuint flags,
        nuint apparent,
        byte[]? complete = null
    )
    {
        lock (gate)
        {
            nint expected = Require();
            if (nextCapacity < 0 || apparent > (nuint)nextCapacity || complete is not null && complete.Length > nextCapacity)
                throw new InvalidOperationException(NativeWritableBinaryLayout.Invalid);
            _ = checked(mutationGeneration + 1);
            nint replacement = 0;
            try
            {
                if (nextCapacity != capacity)
                {
                    replacement = (nint)NativeMemory.AllocZeroed(checked((nuint)sizeof(OrdinaryBinaryHeader) + (nuint)nextCapacity));
                    if (replacement == 0) throw new OutOfMemoryException();
                    var header = (OrdinaryBinaryHeader*)replacement;
                    header->Flags = flags;
                    header->ApparentSize = apparent;
                    header->OriginalSize = nextCapacity;
                    int copied = Math.Min(capacity, nextCapacity);
                    new ReadOnlySpan<byte>((byte*)expected + sizeof(OrdinaryBinaryHeader), copied)
                        .CopyTo(new Span<byte>((byte*)replacement + sizeof(OrdinaryBinaryHeader), nextCapacity));
                    if (complete is not null) complete.AsSpan().CopyTo(new Span<byte>((byte*)replacement + sizeof(OrdinaryBinaryHeader), nextCapacity));
                }

                return new(
                    this,
                    expected,
                    mutationGeneration,
                    nextCapacity,
                    flags,
                    apparent,
                    complete,
                    replacement
                );
            }
            catch
            {
                NativeMemory.Free((void*)replacement);
                throw;
            }
        }
    }

    // Caller owns the exact writer heap gate. Pointer publication to BinRef is
    // callback-free and allocation-free; no owner->heap lock acquisition occurs.
    internal void CommitMutation(OrdinaryBinaryMutation mutation, WritableBinaryBinding binding)
    {
        lock (gate)
        {
            if (!ReferenceEquals(mutation.Owner, this) || Require() != mutation.Expected || mutationGeneration != mutation.Generation
                || !ReferenceEquals(writableBinding, binding) || !Monitor.IsEntered(binding.Heap.Gate))
                throw new InvalidOperationException(NativeWritableBinaryLayout.Invalid);
            long next = checked(mutationGeneration + 1);
            nint replacement = mutation.TakeReplacement();
            nint previous = allocation;
            if (replacement != 0)
            {
                ((OrdinaryBinaryHeader*)replacement)->References = ((OrdinaryBinaryHeader*)previous)->References;
                allocation = replacement;
                capacity = mutation.Capacity;
            }
            else if (mutation.Complete is not null)
                mutation.Complete.AsSpan().CopyTo(new Span<byte>((byte*)allocation + sizeof(OrdinaryBinaryHeader), capacity));
            var header = (OrdinaryBinaryHeader*)allocation;
            header->Flags = mutation.Flags;
            header->ApparentSize = mutation.Apparent;
            mutationGeneration = next;
            binding.BinRef.Block.Slab.Cells.WriteNumeric556(mutation.PointerCell, binding.BinRef.Block.AllocationOffset526 + NativeOffheap576Names.BinaryWord);
            if ((mutation.Flags & NativeWritableBinaryLayout.Writable) == 0) writableBinding = null;
            if (replacement != 0) NativeMemory.Free((void*)previous);
        }
    }

    public void PinWritable()
    {
        while (true)
        {
            WritableBinaryBinding? binding;
            lock (gate)
            {
                if ((((OrdinaryBinaryHeader*)Require())->Flags & NativeWritableBinaryLayout.Writable) == 0) return;
                binding = writableBinding;
                if (binding is null)
                {
                    // Only a producer-owned, unpublished allocation can have no
                    // bound BinRef. Do not perform foreign heap mutation here.
                    ((OrdinaryBinaryHeader*)allocation)->Flags = 0;
                    mutationGeneration = checked(mutationGeneration + 1);

                    return;
                }
            }
            if (binding.Heap.PinWritableBinary(this, binding)) return;
        }
    }
}
