namespace Erlang.NifNativeBackend;

public static class OrdinaryBinaryChecks
{
    private static byte[] Bytes() => Enumerable.Range(0, OrdinaryBinaryCaseNames.Bytes).Select(index => (byte)(index * 37 + 11)).ToArray();

    public static async Task PhysicalBirthAndReclaim()
    {
        await using var runtime = new ProcessRuntime();
        var heap = new ProcessTermHeap506(runtime);
        try
        {
            using var frame = heap.Enter();
            int slot = frame.Reserve();
            byte[] expected = Bytes();
            frame.SetPrepared(slot, new BitString(expected));
            var value = (BitString)frame.Read(slot);
            CpuStatisticsAccess.Require(OrdinaryBinaryTestAccess.Storage(value) is ProcessTermSequence506);
            var storage = (ProcessTermSequence506)OrdinaryBinaryTestAccess.Storage(value)!;
            CpuStatisticsAccess.Require(heap.AllocationSnapshot567.NurseryAllocationWords == OrdinaryBinaryCaseNames.HeaderWords);
            CpuStatisticsAccess.Require(storage.AllocationWords526 == OrdinaryBinaryCaseNames.SubBitsWords);
            OrdinaryBinaryBacking owner = (OrdinaryBinaryBacking)OrdinaryBinaryTestAccess.Owner(value)!;
            CpuStatisticsAccess.Require(ReferenceEquals(owner.Runtime, runtime));
            CpuStatisticsAccess.Require(!owner.Retired.IsCompleted);
            CpuStatisticsAccess.Require(System.Runtime.InteropServices.Marshal.ReadInt64(owner.BinaryPointer, 0) == 0);
            CpuStatisticsAccess.Require(System.Runtime.InteropServices.Marshal.ReadInt64(owner.BinaryPointer, 8) == 0);
            CpuStatisticsAccess.Require(System.Runtime.InteropServices.Marshal.ReadInt64(owner.BinaryPointer, 16) == 1);
            CpuStatisticsAccess.Require(System.Runtime.InteropServices.Marshal.ReadInt64(owner.BinaryPointer, 24) == expected.Length);
            CpuStatisticsAccess.Require(storage.Slab.Cells.ReadHeader526(storage.AllocationOffset526) == 0x124);
            CpuStatisticsAccess.Require(value.ToArray().SequenceEqual(expected));
            CpuStatisticsAccess.Require(OrdinaryBinaryTestAccess.ReferencedBytes(value) == expected.Length);
            Term exported = OwnedTermDetachedCopy506.CopyAtomics586(runtime, value, true);
            CpuStatisticsAccess.Require(exported is BitString exportedBits && OrdinaryBinaryTestAccess.Storage(exportedBits) is null && OrdinaryBinaryTestAccess.Owner(exportedBits) is null);
            CpuStatisticsAccess.Require(heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0);
            frame.Clear(slot);
            ProcessTermCollection508 result = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
            CpuStatisticsAccess.Require(result.ReclaimedPayloadWords == 0 && result.ReclaimedAllocationWords567 == OrdinaryBinaryCaseNames.HeaderWords);
            await owner.Retired.WaitAsync(OrdinaryBinaryCaseNames.Timeout);
            CpuStatisticsAccess.Require(((BitString)exported).ToArray().SequenceEqual(expected));
            bool rejected = false;
            try
            {
                _ = value.ToArray();
            }
            catch (ObjectDisposedException)
            {
                rejected = true;
            }
            CpuStatisticsAccess.Require(rejected);
            GC.KeepAlive(value);
        }
        finally
        {
            await heap.RetireAsync().WaitAsync(OrdinaryBinaryCaseNames.Timeout);
        }
    }

    public static async Task PreserveSmallReferenceShape()
    {
        await using var runtime = new ProcessRuntime();
        var heap = new ProcessTermHeap506(runtime);
        try
        {
            using var frame = heap.Enter();
            BitString input = HeapBitStringTestAccess.ReferenceView(Bytes(), HeapBitStringCaseNames.ReferenceByteOffset, HeapBitStringCaseNames.ReferenceViewBits);
            byte[] expected = input.ToArray();
            int slot = frame.Reserve();
            frame.SetPrepared(slot, input);
            var value = (BitString)frame.Read(slot);
            CpuStatisticsAccess.Require(HeapBitStringTestAccess.Storage(value) is null);
            CpuStatisticsAccess.Require(OrdinaryBinaryTestAccess.Storage(value) is ProcessTermSequence506);
            CpuStatisticsAccess.Require(HeapBitStringTestAccess.HasSubBits(value)
                && OrdinaryBinaryTestAccess.ReferencedBytes(value) == OrdinaryBinaryCaseNames.Bytes);
            CpuStatisticsAccess.Require(heap.AllocationSnapshot567.NurseryAllocationWords == OrdinaryBinaryCaseNames.HeaderWords);
            CpuStatisticsAccess.Require(value.ToArray().SequenceEqual(expected));
            var owner = (OrdinaryBinaryBacking)OrdinaryBinaryTestAccess.Owner(value)!;
            frame.Clear(slot);
            CpuStatisticsAccess.Require(heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567
                == OrdinaryBinaryCaseNames.HeaderWords);
            await owner.Retired.WaitAsync(OrdinaryBinaryCaseNames.Timeout);
            CpuStatisticsAccess.Require(OrdinaryBinaryTestAccess.Owner(input) is null && input.ToArray().SequenceEqual(expected));
            GC.KeepAlive(value);
        }
        finally
        {
            await heap.RetireAsync().WaitAsync(OrdinaryBinaryCaseNames.Timeout);
        }
    }

    public static async Task GraphTransferAndHostEscape()
    {
        await using var runtime = new ProcessRuntime();
        var source = new ProcessTermHeap506(runtime);
        var target = new ProcessTermHeap506(runtime);
        RuntimeProcessPayload641? graph = null;
        try
        {
            using var first = source.Enter();
            using var second = target.Enter();
            int sourceSlot = first.Reserve();
            byte[] expected = Bytes();
            first.SetPrepared(sourceSlot, new BitString(expected));
            var original = (BitString)first.Read(sourceSlot);
            var owner = (OrdinaryBinaryBacking)OrdinaryBinaryTestAccess.Owner(original)!;
            graph = RuntimeProcessPayload641.Prepare641(runtime, null, original);
            CpuStatisticsAccess.Require(graph.Graph641 is not null);
            first.Clear(sourceSlot);
            CpuStatisticsAccess.Require(source.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == OrdinaryBinaryCaseNames.HeaderWords);
            CpuStatisticsAccess.Require(!owner.Retired.IsCompleted);
            CpuStatisticsAccess.Require(((BitString)graph.Value641).ToArray().SequenceEqual(expected));
            int targetSlot = second.Reserve();
            second.SetPrepared(targetSlot, graph.Value641);
            var value = (BitString)second.Read(targetSlot);
            CpuStatisticsAccess.Require(ReferenceEquals(OrdinaryBinaryTestAccess.Owner(value), owner));
            CpuStatisticsAccess.Require(!ReferenceEquals(OrdinaryBinaryTestAccess.Storage(original), OrdinaryBinaryTestAccess.Storage(value)));
            graph.Close641();
            await graph.Completion641.WaitAsync(OrdinaryBinaryCaseNames.Timeout);
            CpuStatisticsAccess.Require(!owner.Retired.IsCompleted && value.ToArray().SequenceEqual(expected));
            Term host = OwnedTermDetachedCopy506.CopyAtomics586(runtime, value, true);
            second.Clear(targetSlot);
            CpuStatisticsAccess.Require(target.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == OrdinaryBinaryCaseNames.HeaderWords);
            await owner.Retired.WaitAsync(OrdinaryBinaryCaseNames.Timeout);
            CpuStatisticsAccess.Require(((BitString)host).ToArray().SequenceEqual(expected));
            GC.KeepAlive(graph);
            GC.KeepAlive(original);
        }
        finally
        {
            graph?.Close641();
            if (graph is not null) await graph.Completion641.WaitAsync(OrdinaryBinaryCaseNames.Timeout);
            await Task.WhenAll(source.RetireAsync(), target.RetireAsync()).WaitAsync(OrdinaryBinaryCaseNames.Timeout);
        }
    }

    public static async Task VmCloseWaitsRealInterest()
    {
        var runtime = new ProcessRuntime();
        var heap = new ProcessTermHeap506(runtime);
        ProcessTermFrame506? frame = heap.Enter();
        Task? shutdown = null;
        try
        {
            int slot = frame.Reserve();
            frame.SetPrepared(slot, new BitString(Bytes()));
            var value = (BitString)frame.Read(slot);
            var owner = (OrdinaryBinaryBacking)OrdinaryBinaryTestAccess.Owner(value)!;
            shutdown = runtime.DisposeAsync().AsTask();
            // The real public dispose stops fresh payload admission before joining.
            var deadline = DateTime.UtcNow + OrdinaryBinaryCaseNames.Timeout;
            while (!runtime.OrdinaryBinaryAdmissionClosed && !shutdown.IsCompleted && DateTime.UtcNow < deadline)
                await Task.Delay(1);
            CpuStatisticsAccess.Require(runtime.OrdinaryBinaryAdmissionClosed && !shutdown.IsCompleted && !owner.Retired.IsCompleted);
            bool rejected = false;
            try
            {
                frame.SetPrepared(frame.Reserve(), new BitString(Bytes()));
            }
            catch (ObjectDisposedException)
            {
                rejected = true;
            }
            catch (ErlangException)
            {
                rejected = true;
            }
            CpuStatisticsAccess.Require(rejected);
            frame.Dispose();
            frame = null;
            await heap.RetireAsync().WaitAsync(OrdinaryBinaryCaseNames.Timeout);
            await shutdown.WaitAsync(OrdinaryBinaryCaseNames.Timeout);
            CpuStatisticsAccess.Require(owner.Retired.IsCompletedSuccessfully);
            GC.KeepAlive(value);
        }
        finally
        {
            frame?.Dispose();
            await heap.RetireAsync().WaitAsync(OrdinaryBinaryCaseNames.Timeout);
            if (shutdown is not null) await shutdown.WaitAsync(OrdinaryBinaryCaseNames.Timeout);
            else await runtime.DisposeAsync();
        }
    }
}
