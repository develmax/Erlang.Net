using System.Reflection;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

public static class HeapFunctionChecks
{
    public static async Task CanonicalSourceBirth()
    {
        foreach (bool owned in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(HeapFunctionCaseNames.Source, HeapFunctionCaseNames.SourceFile);
            await using var runtime = NativeOwned516Profile.Create(owned);
            compiled.Definition.Register(runtime.Modules);
            runtime.Modules.Register(
                HeapFunctionCaseNames.ProbeModule,
                HeapFunctionCaseNames.ProbeFunction,
                0,
                (context, _) => ValueTask.FromResult(Observe(context))
            );
            Term result = await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async context =>
                await runtime.Modules.Call(
                    context,
                    HeapFunctionCaseNames.Module,
                    HeapFunctionCaseNames.Function,
                    Term.I(41)
                )
            );
            CpuStatisticsAccess.Require(result is TupleTerm { Items.Count: 4 });
            var value = (TupleTerm)result;
            CpuStatisticsAccess.Require(value.Items[0].Equals(Term.Tuple(Term.I(0), Term.I(0))));
            CpuStatisticsAccess.Require(value.Items[1].Equals(Term.Tuple(Term.I(1), Term.I(HeapFunctionCaseNames.HeaderWordsWithOneFree))));
            CpuStatisticsAccess.Require(value.Items[2].Equals(Term.Tuple(Term.I(0), Term.I(0))));
            CpuStatisticsAccess.Require(value.Items[3].Equals(Term.Tuple(Term.I(41), Term.I(42))));
            MicrostateTestAccess.Capture(HeapFunctionCaseNames.Module, result);
        }
    }

    internal static Term Observe(ProcessContext context)
    {
        // Real public opaque zero-argument callback: no canonical permission,
        // guessed actor, fake guest frame, private writes or Header counter.
        ProcessTermHeap506 heap = context.TermHeap506;
        var blocks = (Dictionary<long, ProcessTermSequence506>)typeof(ProcessTermHeap506)
            .GetField(HeapFunctionCaseNames.HeapBlocks, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(heap)!;
        int count = 0;
        long words = 0;
        lock (heap.Gate)
            foreach (var block in blocks.Values)
            {
                if (block.HeaderWords526 != 1 || block.RawPayloadWords556 != 1) continue;
                nuint header = block.Slab.Cells.ReadHeader526(block.AllocationOffset526);
                if ((header & byte.MaxValue) != HeapFunctionCaseNames.FunSubtag) continue;
                int environment = (int)(header >> HeapFunctionCaseNames.EnvironmentShift) & byte.MaxValue;
                int arity = (int)(header >> HeapFunctionCaseNames.ArityShift) & byte.MaxValue;
                CpuStatisticsAccess.Require(environment == 1 && arity == 0 && block.Count == 1);
                CpuStatisticsAccess.Require(block.AllocationWords526 == HeapFunctionCaseNames.HeaderWordsWithOneFree);
                CpuStatisticsAccess.Require(block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + 1) != 0);
                CpuStatisticsAccess.Require(block[0] is TupleTerm { Items.Count: 2 });
                count++;
                words += block.AllocationWords526;
            }

        return Term.Tuple(Term.I(count), Term.I(words));
    }
}
