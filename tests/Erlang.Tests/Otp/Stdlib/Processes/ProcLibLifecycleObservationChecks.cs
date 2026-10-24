using Erlang.Differential;

namespace Erlang.Tests;

public static class ProcLibLifecycleObservationChecks
{
    public static async Task Run()
    {
        await ProcLibLifecycleMfaContracts.ParseAll();
        foreach (var row in ProcLibLifecycleCaseData.Observational)
        {
            using var artifact = GeneratedModuleCompiler.Compile(row.Source);
            Term actual = await CompiledModuleExecution.Run(artifact);
            if (actual is not TupleTerm { Items: [Atom { Name: ProcLibLifecycleCaseNames.Ok }, TupleTerm { Items: [Atom { Name: ProcLibLifecycleCaseNames.Ok }, var trace] }] })
                throw new InvalidOperationException(ProcLibLifecycleCaseNames.Mismatch + row.Name + actual);
            _ = Cons.Items(trace).ToArray();
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { row.Name, SourceInvariantsPassed = true, OriginalCompared = false, RawEtfBase64 = Convert.ToBase64String(ExternalTermFormat.Encode(actual)) }));
        }
    }
}
