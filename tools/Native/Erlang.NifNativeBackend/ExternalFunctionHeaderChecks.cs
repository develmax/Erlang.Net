using System.Reflection;

namespace Erlang.NifNativeBackend;

public static class ExternalFunctionHeaderChecks
{
    public static async Task OpaqueExternalMetadataKind()
    {
        await using var runtime = new ProcessRuntime();
        var heap = new ProcessTermHeap506(runtime);
        try
        {
            using var frame = heap.Enter();
            var identity = new ExternalFunctionIdentity(Term.A(ExternalFunctionHeaderCaseNames.Module), Term.A(ExternalFunctionHeaderCaseNames.Function), 1);
            var sdk = new FunctionTerm(identity, (_, arguments) => ValueTask.FromResult(arguments[0]));
            int slot = frame.Reserve();
            frame.SetPrepared(slot, sdk);
            var actual = (FunctionTerm)frame.Read(slot);
            var storage = typeof(FunctionTerm).GetProperty(ExternalFunctionHeaderCaseNames.Storage, BindingFlags.Instance | BindingFlags.NonPublic)!;
            CpuStatisticsAccess.Require(storage.GetValue(actual) is ProcessTermSequence506 && storage.GetValue(sdk) is null);
            var block = (ProcessTermSequence506)storage.GetValue(actual)!;
            CpuStatisticsAccess.Require(actual.Arity == 1 && actual.Environment.Count == 0 && actual.ExternalIdentity == identity
                && block.AllocationWords526 == 2 && block.Count == 0
                && block.Slab.Cells.ReadHeader526(block.AllocationOffset526) == ExternalFunctionHeaderCaseNames.Header);
            CpuStatisticsAccess.Require(!runtime.Functions.IsExternalEntry(actual)
                && !RuntimeStoredFunctionRecipes.TryCaptureTransfer506(runtime, actual, out _));
            CpuStatisticsAccess.Require(heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0);
            frame.Clear(slot);
            CpuStatisticsAccess.Require(heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 2);
            GC.KeepAlive(actual);
            GC.KeepAlive(sdk);
        }
        finally
        {
            await heap.RetireAsync().WaitAsync(ExternalFunctionHeaderCaseNames.Timeout);
        }
    }
}
