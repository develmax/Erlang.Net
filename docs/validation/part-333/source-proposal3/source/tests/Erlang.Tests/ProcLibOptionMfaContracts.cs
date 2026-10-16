using Erlang.Differential;

namespace Erlang.Tests;

public static class ProcLibOptionMfaContracts
{
    public static bool Owns(string module, string function, int arity) => module == ProcLibOptionCaseNames.Module &&
        (function is ProcLibOptionCaseNames.StartMonitor or ProcLibOptionCaseNames.SpawnOpt || arity == 5 && function is ProcLibStartupCaseNames.Start or ProcLibStartupCaseNames.StartLink);

    public static async Task RunAll()
    {
        foreach (var fixture in ProcLibOptionCases.Modules)
        {
            using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
            Term actual = await CompiledModuleExecution.Run(artifact);
            bool passed = actual.Equals(fixture.Expected);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { fixture.Name, Expected = fixture.Expected.ToString(), Actual = actual.ToString(), Passed = passed }));
            if (!passed)
                throw new InvalidOperationException(ProcLibOptionCaseNames.Mismatch);
        }
        (string Function, int Arity)[] exports = [
            (ProcLibOptionCaseNames.StartMonitor, 3),
            (ProcLibOptionCaseNames.StartMonitor, 4),
            (ProcLibOptionCaseNames.StartMonitor, 5),
            (ProcLibStartupCaseNames.Start, 5),
            (ProcLibStartupCaseNames.StartLink, 5),
            (ProcLibOptionCaseNames.SpawnOpt, 2),
            (ProcLibOptionCaseNames.SpawnOpt, 4)
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
            (ProcLibOptionCaseNames.StartMonitor, 3) => ProcLibOptionCaseNames.MonitorThree,
            (ProcLibOptionCaseNames.StartMonitor, 4) => ProcLibOptionCaseNames.MonitorFour,
            (ProcLibOptionCaseNames.StartMonitor, 5) => ProcLibOptionCaseNames.MonitorFive,
            (ProcLibStartupCaseNames.Start, 5) => ProcLibOptionCaseNames.StartFive,
            (ProcLibStartupCaseNames.StartLink, 5) => ProcLibOptionCaseNames.LinkFive,
            (ProcLibOptionCaseNames.SpawnOpt, 2) => ProcLibOptionCaseNames.SpawnFun,
            (ProcLibOptionCaseNames.SpawnOpt, 4) => ProcLibOptionCaseNames.SpawnMfa,
            _ => throw new InvalidOperationException(ProcLibOptionCaseNames.Mismatch)
        };
        var fixture = ProcLibOptionCases.Modules.Single(value => value.Name == name);
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        Term actual = await CompiledModuleExecution.Run(artifact);
        if (!actual.Equals(fixture.Expected))
            throw new InvalidOperationException(ProcLibOptionCaseNames.Mismatch);
    }
}
