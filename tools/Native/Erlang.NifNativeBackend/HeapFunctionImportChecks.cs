using Erlang.Differential;

namespace Erlang.NifNativeBackend;

public static class HeapFunctionImportChecks
{
    public static async Task SourceImportedEnvironment()
    {
        foreach (bool owned in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(HeapFunctionImportCaseNames.Source, HeapFunctionImportCaseNames.SourceFile);
            await using var runtime = NativeOwned516Profile.Create(owned);
            compiled.Definition.Register(runtime.Modules);
            runtime.Modules.Register(
                HeapFunctionCaseNames.ProbeModule,
                HeapFunctionCaseNames.ProbeFunction,
                0,
                (context, _) => ValueTask.FromResult(HeapFunctionChecks.Observe(context))
            );
            Term full = await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async context =>
            {
                Term hostFunction = await runtime.Modules.Call(
                    context,
                    HeapFunctionImportCaseNames.Module,
                    HeapFunctionImportCaseNames.Make,
                    Term.I(41)
                );
                CpuStatisticsAccess.Require(hostFunction is FunctionTerm { Arity: 0 });
                Term result = await runtime.Modules.Call(
                    context,
                    HeapFunctionImportCaseNames.Module,
                    HeapFunctionImportCaseNames.Take,
                    hostFunction
                );
                CpuStatisticsAccess.Require(result is TupleTerm { Items.Count: 4 });
                var value = (TupleTerm)result;
                RequireHeaders(value.Items[0]);
                RequireHeaders(value.Items[1]);
                CpuStatisticsAccess.Require(value.Items[2].Equals(Term.Tuple(Term.I(41), Term.I(42))));
                CpuStatisticsAccess.Require(value.Items[3].Equals(value.Items[2]));
                _ = await runtime.Modules.Call(context, ErlangModuleNames.Module, ProcessGarbageCollectionNames.Function);
                Term after = HeapFunctionChecks.Observe(context);
                CpuStatisticsAccess.Require(after.Equals(Term.Tuple(Term.I(0), Term.I(0))));

                return Term.Tuple(result, after);
            }
            );
            MicrostateTestAccess.Capture(HeapFunctionImportCaseNames.Module, full);
        }
    }

    private static void RequireHeaders(Term snapshot)
    {
        CpuStatisticsAccess.Require(snapshot is TupleTerm { Items: [Integer count, Integer words] }
            && count.Value > 0 && words.Value == count.Value * HeapFunctionCaseNames.HeaderWordsWithOneFree);
    }
}
