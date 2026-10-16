using Erlang.Differential;

namespace Erlang.Tests;

public static class ProcLibLifecycleMfaContracts
{
    public static bool Owns(string module, string function, int arity)
        => ProcLibLifecycleCaseData.Direct.Any(row => row.Module == module && row.Function == function && row.Arity == arity);

    public static async Task Run(string module, string function, int arity)
    {
        var row = ProcLibLifecycleCaseData.Direct.Single(value => value.Module == module && value.Function == function && value.Arity == arity);
        using var artifact = GeneratedModuleCompiler.Compile(row.Source);
        Term actual = await CompiledModuleExecution.Run(artifact);
        Term expected = Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(ProcLibLifecycleCaseNames.Ok));
        if (!actual.Equals(expected))
            throw new InvalidOperationException(ProcLibLifecycleCaseNames.Mismatch + module + function + arity);
    }

    public static async Task ParseAll()
    {
        var failures = new List<Exception>();
        int parsed = 0;
        foreach (var row in ProcLibLifecycleCaseData.Asserted
            .Concat(ProcLibLifecycleCaseData.Observational)
            .Concat(ProcLibLifecycleCaseData.Direct.Select(row => (Name: row.Module + row.Function + row.Arity, Source: row.Source))))
        {
            try
            {
                new Erlang.Compiler.Parser(row.Source).ParseModule();
                parsed++;
            }
            catch (Erlang.Compiler.CompileException exception)
            {
                int start = Math.Max(0, exception.Offset - 80);
                string context = row.Source.Substring(start, Math.Min(160, row.Source.Length - start));
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { row.Name, exception.Code, exception.Offset, exception.Message, SourceContext = context }));
                failures.Add(new InvalidOperationException(row.Name, exception));
            }
        }
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { ParsedSources = parsed, Failures = failures.Count, GuestCasesExecuted = 0 }));
        if (failures.Count != 0)
            throw new AggregateException(failures);

        await Task.CompletedTask;
    }
}
