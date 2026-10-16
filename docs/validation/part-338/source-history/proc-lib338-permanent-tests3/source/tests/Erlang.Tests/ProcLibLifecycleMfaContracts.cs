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
        Term expected = Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibLifecycleCaseNames.Ok));
        if (!actual.Equals(expected))
            throw new InvalidOperationException(ProcLibLifecycleCaseNames.Mismatch + module + function + arity);
    }

    public static async Task ParseAll()
    {
        foreach (string source in ProcLibLifecycleCaseData.Asserted.Select(row => row.Source)
            .Concat(ProcLibLifecycleCaseData.Observational.Select(row => row.Source))
            .Concat(ProcLibLifecycleCaseData.Direct.Select(row => row.Source)))
            new Erlang.Compiler.Parser(source).ParseModule();

        await Task.CompletedTask;
    }
}
