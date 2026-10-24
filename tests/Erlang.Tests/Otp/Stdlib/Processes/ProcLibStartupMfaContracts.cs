using Erlang.Differential;

namespace Erlang.Tests;

public static class ProcLibStartupMfaContracts
{
    public static bool Owns(string module, string function) => module == ProcLibStartupCaseNames.Module && function is ProcLibStartupCaseNames.Start or ProcLibStartupCaseNames.StartLink or ProcLibStartupCaseNames.InitFail;

    public static async Task RunAll()
    {
        foreach (var fixture in ProcLibStartupCases.Modules.Concat(ReceiveWaitCases.Modules))
        {
            using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
            Term actual = await CompiledModuleExecution.Run(artifact);
            bool passed = actual.Equals(fixture.Expected);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { fixture.Name, Expected = fixture.Expected.ToString(), Actual = actual.ToString(), Passed = passed }));
            if (!passed)
                throw new InvalidOperationException($"{ProcLibStartupCaseNames.Mismatch}: {fixture.Name}");
        }
        (string Function, int Arity)[] exports = [
            (ProcLibStartupCaseNames.Start, 3),
            (ProcLibStartupCaseNames.Start, 4),
            (ProcLibStartupCaseNames.StartLink, 3),
            (ProcLibStartupCaseNames.StartLink, 4),
            (ProcLibStartupCaseNames.InitFail, 2),
            (ProcLibStartupCaseNames.InitFail, 3)
        ];
        foreach (var export in exports)
        {
            await Run(export.Function, export.Arity);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { export.Function, export.Arity, Direct = true, Passed = true }));
        }
    }

    public static async Task Run(string function, int arity)
    {
        string name = (function, arity) switch
        {
            (ProcLibStartupCaseNames.Start, 3) => ProcLibStartupCaseNames.StartThree,
            (ProcLibStartupCaseNames.Start, 4) => ProcLibStartupCaseNames.StartFour,
            (ProcLibStartupCaseNames.StartLink, 3) => ProcLibStartupCaseNames.LinkThree,
            (ProcLibStartupCaseNames.StartLink, 4) => ProcLibStartupCaseNames.LinkFour,
            (ProcLibStartupCaseNames.InitFail, 2) => ProcLibStartupCaseNames.FailTwo,
            (ProcLibStartupCaseNames.InitFail, 3) => ProcLibStartupCaseNames.FailThree,
            _ => throw new InvalidOperationException(ProcLibStartupCaseNames.MissingContract)
        };
        var fixture = ProcLibStartupCases.Modules.Single(value => value.Name == name);
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        Term actual = await CompiledModuleExecution.Run(artifact);
        if (!actual.Equals(fixture.Expected))
            throw new InvalidOperationException(ProcLibStartupCaseNames.Mismatch);
    }
}
