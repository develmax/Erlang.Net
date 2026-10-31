using System.Numerics;
using Erlang.Differential;
using static Erlang.NifNativeBackend.NativeRecordHeapTestAccess;

namespace Erlang.NifNativeBackend;

public static class NativeRecordHeapChecks
{
    public static async Task PhysicalDefinitionAndReclaim()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = NativeOwned516Profile.Create(owned);
            await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async context =>
            {
                var heap = new ProcessTermHeap506(runtime);
                try
                {
                    using var frame = heap.Enter();
                    int slot = frame.Reserve();
                    NativeRecordTerm input = Input();
                    frame.SetPrepared(slot, input);
                    var value = (NativeRecordTerm)frame.Read(slot);
                    ProcessTermSequence506 block = Inspect(heap, value);
                    Require(value.Equals(input) && !ReferenceEquals(value.Values[0], input.Values[0]) && !ReferenceEquals(value.Values[1], input.Values[1]));
                    TupleTerm definition = heap.ReadRecordDefinition(block);
                    var order = (TupleTerm)definition.Items[0];
                    Require(order.Items[0].Equals(Term.I(1)) && order.Items[1].Equals(Term.I(0))
                        && definition.Items[5].Equals(Term.A(NativeRecordHeapCaseNames.FieldA))
                        && definition.Items[6].Equals(Term.A(NativeRecordHeapCaseNames.FieldZ))
                        && block[0].Equals(value.Values[1]) && block[1].Equals(value.Values[0]));
                    Require(heap.TryReadPhysicalSize526(
                        context,
                        value,
                        false,
                        out ulong words
                    ) && words == NativeRecordHeapCaseNames.LocalTwoFieldWords);
                    Require(heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0);
                    NativeRecordTerm escaped = (NativeRecordTerm)OwnedTermDetachedCopy506.CopyAtomics586(runtime, value, true);
                    Require(Storage(escaped) is null && escaped.Equals(input));
                    frame.Clear(slot);
                    var collected = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                    Require(collected.ReclaimedAllocationWords567 == NativeRecordHeapCaseNames.LocalTwoFieldWords && ExistingRecords(heap).Count == 0);
                    RequireStale(() => _ = value.Module);
                    RequireStale(() => _ = value.Values);
                    Require(escaped.Equals(input));
                    GC.KeepAlive(value);
                }
                finally
                {
                    await heap.RetireAsync().WaitAsync(NativeRecordHeapCaseNames.Timeout);
                }

                return Nil.Value;
            }
            );
        }
    }

    public static async Task TransferAndEscape()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = NativeOwned516Profile.Create(owned);
            var source = new ProcessTermHeap506(runtime);
            var target = new ProcessTermHeap506(runtime);
            try
            {
                using var first = source.Enter();
                using var second = target.Enter();
                int a = first.Reserve(), b = second.Reserve();
                NativeRecordTerm input = Input();
                first.SetPrepared(a, input);
                var original = (NativeRecordTerm)first.Read(a);
                using (var transfer = source.Prepare(first, original)) second.SetPrepared(b, transfer.Portable);
                var copied = (NativeRecordTerm)second.Read(b);
                ProcessTermSequence506 firstBlock = Inspect(source, original), secondBlock = Inspect(target, copied);
                Require(!ReferenceEquals(firstBlock, secondBlock) && copied.Equals(input)
                    && !ReferenceEquals(copied.Values[0], original.Values[0]) && !ReferenceEquals(copied.Values[1], original.Values[1]));
                first.Clear(a);
                Require(source.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == NativeRecordHeapCaseNames.LocalTwoFieldWords);
                RequireStale(() => _ = original.Name);
                Require(copied.Equals(input) && target.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0);
                NativeRecordTerm escaped = (NativeRecordTerm)OwnedTermDetachedCopy506.CopyAtomics586(runtime, copied, true);
                second.Clear(b);
                Require(target.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == NativeRecordHeapCaseNames.LocalTwoFieldWords);
                RequireStale(() => _ = copied.Values);
                Require(Storage(escaped) is null && escaped.Equals(input));
                GC.KeepAlive(original);
                GC.KeepAlive(copied);
            }
            finally
            {
                await Task.WhenAll(source.RetireAsync(), target.RetireAsync()).WaitAsync(NativeRecordHeapCaseNames.Timeout);
            }
        }
    }

    public static async Task SourceBirthAndUpdate()
    {
        foreach (bool owned in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(NativeRecordHeapCaseNames.Program, NativeRecordHeapCaseNames.File);
            await using var runtime = NativeOwned516Profile.Create(owned);
            compiled.Definition.Register(runtime.Modules);
            var phases = new List<int>();
            runtime.Modules.Register(
                NativeRecordHeapCaseNames.Probe,
                NativeRecordHeapCaseNames.Check,
                1,
                (context, arguments) =>
                {
                    int phase = checked((int)((Integer)arguments[0]).Value);
                    var blocks = ExistingRecords(context.TermHeap506);
                    Require(blocks.Count == phase);
                    foreach (var block in blocks)
                    {
                        Require(block.Slab.Cells.ReadHeader526(block.AllocationOffset526) == NativeRecordHeapCaseNames.TwoFieldHeader
                            && context.TermHeap506.ReadRecordModule(block).Name == NativeRecordHeapCaseNames.Module
                            && context.TermHeap506.ReadRecordName(block).Name == NativeRecordHeapCaseNames.Record
                            && context.TermHeap506.ReadRecordExported(block)
                            && context.TermHeap506.ReadRecordValues(block).Count == 2);
                    }
                    phases.Add(phase);

                    return ValueTask.FromResult<Term>(Term.A(NativeRecordHeapCaseNames.Ok));
                }
            );
            var input = new Integer(BigInteger.One << 200);
            Term result = await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async context =>
                await runtime.Modules.Call(
                    context,
                    NativeRecordHeapCaseNames.Module,
                    NativeRecordHeapCaseNames.Run,
                    input
                )
            );
            Require(phases.SequenceEqual(new[] { 1, 2 }) && result is TupleTerm { Items.Count: 3 });
            var tuple = (TupleTerm)result;
            Require(tuple.Items[0] is NativeRecordTerm first && tuple.Items[1] is NativeRecordTerm second
                && Storage(first) is null && Storage(second) is null
                && first.Values[0].Equals(input) && second.Values[0].Equals(new Integer(input.Value + 1))
                && tuple.Items[2].Equals(input));
        }
    }

    public static async Task CloseWaitingSource()
    {
        foreach (bool owned in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(NativeRecordHeapCaseNames.Program, NativeRecordHeapCaseNames.File);
            var runtime = NativeOwned516Profile.Create(owned);
            compiled.Definition.Register(runtime.Modules);
            var ready = new TaskCompletionSource<(ProcessTermHeap506 Heap, ProcessTermSequence506 Record, Task Unwound)>(TaskCreationOptions.RunContinuationsAsynchronously);
            runtime.Modules.Register(
                NativeRecordHeapCaseNames.Probe,
                NativeRecordHeapCaseNames.Check,
                1,
                (context, _) =>
                {
                    var blocks = ExistingRecords(context.TermHeap506);
                    Require(blocks.Count >= 1);
                    ready.TrySetResult((context.TermHeap506, blocks[0], context.Unwound.Task));

                    return ValueTask.FromResult<Term>(Term.A(NativeRecordHeapCaseNames.Ok));
                }
            );
            ProcessHandle actor = runtime.Spawn(async context => await runtime.Modules.Call(
                context,
                NativeRecordHeapCaseNames.Module,
                NativeRecordHeapCaseNames.Wait,
                new Integer(BigInteger.One << 200)
            ));
            try
            {
                var actual = await ready.Task.WaitAsync(NativeRecordHeapCaseNames.Timeout);
                await runtime.DisposeAsync().AsTask().WaitAsync(NativeRecordHeapCaseNames.Timeout);
                await actor.Completion.WaitAsync(NativeRecordHeapCaseNames.Timeout);
                await actual.Unwound.WaitAsync(NativeRecordHeapCaseNames.Timeout);
                RequireStale(() => _ = actual.Heap.ReadRecordModule(actual.Record));
            }
            finally
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(NativeRecordHeapCaseNames.Timeout);
            }
        }
    }
}
