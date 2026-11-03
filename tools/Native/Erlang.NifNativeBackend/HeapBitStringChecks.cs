namespace Erlang.NifNativeBackend;

public static class HeapBitStringChecks
{
    public static async Task PhysicalBirthAndReclaim()
    {
        await using var runtime = new ProcessRuntime();
        var heap = new ProcessTermHeap506(runtime);
        try
        {
            using var frame = heap.Enter();
            foreach (HeapBitStringVector vector in HeapBitStringCaseNames.Vectors)
            {
                int bits = vector.BitLength;
                byte[] input = Enumerable.Range(0, (bits + HeapBitStringCaseNames.ByteRounding) / HeapBitStringCaseNames.BitsPerByte).Select(index => (byte)(index * HeapBitStringCaseNames.PatternStep + HeapBitStringCaseNames.PatternOrigin)).ToArray();
                var portable = new BitString(input, bits);
                byte[] expected = portable.ToArray();
                int slot = frame.Reserve();
                ProcessTermAllocationSnapshot567 before = heap.AllocationSnapshot567;
                frame.SetPrepared(slot, portable);
                var value = (BitString)frame.Read(slot);
                Require(HeapBitStringTestAccess.Storage(value) is ProcessTermSequence506);
                var actual = (ProcessTermSequence506)HeapBitStringTestAccess.Storage(value)!;
                int words = vector.AllocationWords;
                Require(actual.Count == 0 && actual.AllocationWords526 == words);
                Require(actual.Slab.Cells.ReadHeader526(actual.AllocationOffset526) == vector.Header);
                Require(heap.AllocationSnapshot567.NurseryAllocationWords - before.NurseryAllocationWords == words);
                Require(value.BitLength == bits && value.ToArray().SequenceEqual(expected));
                for (int index = 0; index < bits; index++)
                    Require(HeapBitStringTestAccess.Bit(value, index) == HeapBitStringTestAccess.Bit(portable, index));
                Term detached = OwnedTermDetachedCopy506.CopyAtomics586(runtime, value, false);
                Require(detached is BitString detachedBits && HeapBitStringTestAccess.Storage(detachedBits) is null
                    && HeapBitStringTestAccess.ResourceStorage(detachedBits) is null);
                Require(heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0);
                frame.Clear(slot);
                ProcessTermCollection508 collected = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                Require(collected.ReclaimedPayloadWords == 0 && collected.ReclaimedAllocationWords567 == words);
                Require(((BitString)detached).ToArray().SequenceEqual(expected));
                bool staleRejected = false;
                try
                {
                    _ = value.ToArray();
                }
                catch (ObjectDisposedException)
                {
                    staleRejected = true;
                }
                Require(staleRejected);
                GC.KeepAlive(value);
            }
        }
        finally
        {
            await heap.RetireAsync().WaitAsync(HeapBitStringCaseNames.Timeout);
        }
    }

    public static async Task TransferAndEscape()
    {
        await using var runtime = new ProcessRuntime();
        var source = new ProcessTermHeap506(runtime);
        var target = new ProcessTermHeap506(runtime);
        try
        {
            using var sourceFrame = source.Enter();
            using var targetFrame = target.Enter();
            int slot = sourceFrame.Reserve();
            sourceFrame.SetPrepared(slot, new BitString(HeapBitStringCaseNames.TransferBytes, HeapBitStringCaseNames.TransferBits));
            var original = (BitString)sourceFrame.Read(slot);
            BitString selected = original.SliceBits(HeapBitStringCaseNames.SliceOffset, HeapBitStringCaseNames.SliceBits);
            byte[] expected = selected.ToArray();
            int selectedSlot = sourceFrame.Reserve();
            sourceFrame.SetPrepared(selectedSlot, selected);
            var backed = (BitString)sourceFrame.Read(selectedSlot);
            using (PreparedTermTransfer506 transfer = source.Prepare(sourceFrame, backed))
            {
                int targetSlot = targetFrame.Reserve();
                targetFrame.SetPrepared(targetSlot, transfer.Portable);
                var result = (BitString)targetFrame.Read(targetSlot);
                Require(HeapBitStringTestAccess.Storage(result) is ProcessTermSequence506 block && ReferenceEquals(block.Owner, target));
                Require(!ReferenceEquals(HeapBitStringTestAccess.Storage(backed), HeapBitStringTestAccess.Storage(result)));
                sourceFrame.Clear(slot);
                sourceFrame.Clear(selectedSlot);
                // Prepared transfer retains the real source borrow until actual handoff ends.
                Require(source.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 > 0);
                Require(result.ToArray().SequenceEqual(expected) && result.BitLength == HeapBitStringCaseNames.SliceBits);
                Require(target.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0);
                targetFrame.Clear(targetSlot);
            }
            Require(source.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 > 0);
            Require(target.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567
                == HeapBitStringCaseNames.SliceAllocationWords);
        }
        finally
        {
            await Task.WhenAll(source.RetireAsync(), target.RetireAsync()).WaitAsync(HeapBitStringCaseNames.Timeout);
        }
    }

    public static async Task PreserveReferenceShape()
    {
        await using var runtime = new ProcessRuntime();
        var heap = new ProcessTermHeap506(runtime);
        try
        {
            using var frame = heap.Enter();
            byte[] parent = Enumerable.Range(0, HeapBitStringCaseNames.ReferenceParentBytes).Select(index => (byte)index).ToArray();
            BitString input = HeapBitStringTestAccess.ReferenceView(parent, HeapBitStringCaseNames.ReferenceByteOffset, HeapBitStringCaseNames.ReferenceViewBits);
            Require(HeapBitStringTestAccess.HasSubBits(input));
            int slot = frame.Reserve();
            frame.SetPrepared(slot, input);
            var value = (BitString)frame.Read(slot);
            Require(HeapBitStringTestAccess.Storage(value) is null
                && HeapBitStringTestAccess.HasSubBits(value)
                && HeapBitStringTestAccess.ReferencedBytes(value) == parent.Length
                && value.ToArray().SequenceEqual(input.ToArray()));
        }
        finally
        {
            await heap.RetireAsync().WaitAsync(HeapBitStringCaseNames.Timeout);
        }
    }


    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(HeapBitStringCaseNames.Failure);
    }

}
