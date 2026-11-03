namespace Erlang.NifNativeBackend;

public static class ExternalIdentifierChecks
{
    public static async Task PhysicalBirthAndReclaim()
    {
        await using var runtime = new ProcessRuntime();
        var heap = new ProcessTermHeap506(runtime);
        try
        {
            using var frame = heap.Enter();
            foreach (ExternalIdentifierVector vector in ExternalIdentifierVector.Values)
            {
                int slot = frame.Reserve();
                Term expected = ExternalIdentifierTestAccess.Snapshot(vector.Input);
                frame.SetPrepared(slot, vector.Input);
                Term value = frame.Read(slot);
                CpuStatisticsAccess.Require(ExternalIdentifierTestAccess.Storage(value) is ProcessTermSequence506);
                var block = (ProcessTermSequence506)ExternalIdentifierTestAccess.Storage(value)!;
                CpuStatisticsAccess.Require(block.Count == 0 && block.AllocationWords526 == 3 + vector.Data.Length
                    && block.Slab.Cells.ReadHeader526(block.AllocationOffset526) == vector.Header);
                for (int index = 0; index < vector.Data.Length; index++)
                    CpuStatisticsAccess.Require(block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + 3 + index) == vector.Data[index]);
                NativeOffheapHeader576 entry;
                lock (heap.Gate) entry = heap.RequireNativeOffheapHeader576(block, vector.Kind);
                CpuStatisticsAccess.Require(entry.ExternalNode is not null && entry.Binary != 0
                    && block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + 1) == (nuint)entry.ExternalNode.Address
                    && entry.ExternalNode.Read().References == 1);
                CpuStatisticsAccess.Require(ExternalIdentifierTestAccess.Snapshot(value).Equals(expected));
                CpuStatisticsAccess.Require(heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0);
                Term escaped = OwnedTermDetachedCopy506.CopyAtomics586(runtime, value, true);
                CpuStatisticsAccess.Require(ExternalIdentifierTestAccess.Storage(escaped) is null);
                frame.Clear(slot);
                var collected = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                CpuStatisticsAccess.Require(collected.ReclaimedPayloadWords == 0
                    && collected.ReclaimedAllocationWords567 == 3 + vector.Data.Length);
                CpuStatisticsAccess.Require(ExternalIdentifierTestAccess.NodeCount(runtime) == 0
                    && ExternalIdentifierTestAccess.Snapshot(escaped).Equals(expected));
                bool stale = false;
                try
                {
                    _ = ExternalIdentifierTestAccess.Snapshot(value);
                }
                catch (ObjectDisposedException)
                {
                    stale = true;
                }
                CpuStatisticsAccess.Require(stale);
                GC.KeepAlive(value);
            }
        }
        finally
        {
            await heap.RetireAsync().WaitAsync(ExternalIdentifierCaseNames.Timeout);
        }
    }

    public static async Task CrossHeapLeaseAndHostEscape()
    {
        await using var runtime = new ProcessRuntime();
        var source = new ProcessTermHeap506(runtime);
        var target = new ProcessTermHeap506(runtime);
        try
        {
            using var first = source.Enter();
            using var second = target.Enter();
            int sourceSlot = first.Reserve(), targetSlot = second.Reserve();
            var vector = ExternalIdentifierVector.Values[0];
            first.SetPrepared(sourceSlot, vector.Input);
            Term original = first.Read(sourceSlot);
            using (var transfer = source.Prepare(first, original)) second.SetPrepared(targetSlot, transfer.Portable);
            Term copied = second.Read(targetSlot);
            var sourceBlock = (ProcessTermSequence506)ExternalIdentifierTestAccess.Storage(original)!;
            var targetBlock = (ProcessTermSequence506)ExternalIdentifierTestAccess.Storage(copied)!;
            NativeOffheapHeader576 sourceEntry, targetEntry;
            lock (source.Gate) sourceEntry = source.RequireNativeOffheapHeader576(sourceBlock, vector.Kind);
            lock (target.Gate) targetEntry = target.RequireNativeOffheapHeader576(targetBlock, vector.Kind);
            CpuStatisticsAccess.Require(!ReferenceEquals(sourceBlock, targetBlock) && sourceEntry.Binary == targetEntry.Binary
                && sourceEntry.ExternalNode!.Read().References == 2);
            first.Clear(sourceSlot);
            CpuStatisticsAccess.Require(source.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 4);
            CpuStatisticsAccess.Require(targetEntry.ExternalNode!.Read().References == 1
                && ExternalIdentifierTestAccess.Snapshot(copied).Equals(ExternalIdentifierTestAccess.Snapshot(vector.Input)));
            Term escaped = OwnedTermDetachedCopy506.CopyAtomics586(runtime, copied, true);
            second.Clear(targetSlot);
            CpuStatisticsAccess.Require(target.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 4);
            CpuStatisticsAccess.Require(ExternalIdentifierTestAccess.NodeCount(runtime) == 0
                && ExternalIdentifierTestAccess.Snapshot(escaped).Equals(ExternalIdentifierTestAccess.Snapshot(vector.Input)));
            GC.KeepAlive(original);
            GC.KeepAlive(copied);
        }
        finally
        {
            await Task.WhenAll(source.RetireAsync(), target.RetireAsync()).WaitAsync(ExternalIdentifierCaseNames.Timeout);
        }
    }

    public static async Task VmCloseWaitsForRealNode()
    {
        var runtime = new ProcessRuntime();
        var heap = new ProcessTermHeap506(runtime);
        ProcessTermFrame506? frame = heap.Enter();
        Task? dispose = null;
        try
        {
            int slot = frame.Reserve();
            frame.SetPrepared(slot, ExternalIdentifierVector.Values[0].Input);
            Term actual = frame.Read(slot);
            dispose = runtime.DisposeAsync().AsTask();
            CpuStatisticsAccess.Require(!dispose.IsCompleted && ExternalIdentifierTestAccess.NodeCount(runtime) == 1);
            bool refused = false;
            try
            {
                using var rejected = new PreparedExternalIdentifier(runtime, ExternalIdentifierVector.Values[0].Input);
            }
            catch (ObjectDisposedException)
            {
                refused = true;
            }
            CpuStatisticsAccess.Require(refused);
            frame.Dispose();
            frame = null;
            await heap.RetireAsync().WaitAsync(ExternalIdentifierCaseNames.Timeout);
            await dispose.WaitAsync(ExternalIdentifierCaseNames.Timeout);
            CpuStatisticsAccess.Require(ExternalIdentifierTestAccess.NodeCount(runtime) == 0);
            GC.KeepAlive(actual);
        }
        finally
        {
            frame?.Dispose();
            await heap.RetireAsync().WaitAsync(ExternalIdentifierCaseNames.Timeout);
            await (dispose ?? runtime.DisposeAsync().AsTask()).WaitAsync(ExternalIdentifierCaseNames.Timeout);
        }
    }
}
