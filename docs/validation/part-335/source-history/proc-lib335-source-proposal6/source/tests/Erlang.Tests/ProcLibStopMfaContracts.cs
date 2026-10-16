using Erlang.Differential;

namespace Erlang.Tests;

public static class ProcLibStopMfaContracts
{
    public static bool Owns(string module, string function, int arity)
        => module == ProcLibStopCaseNames.Module && function == ProcLibStopCaseNames.Stop && arity is 1 or 3 ||
           module == ProcLibStopCaseNames.Sys && function == ProcLibStopCaseNames.Terminate && arity is 2 or 3;

    public static async Task ParseAll()
    {
        foreach (var fixture in ProcLibStopCases.Modules)
            new Erlang.Compiler.Parser(fixture.Source).ParseModule();

        await Task.CompletedTask;
    }

    public static async Task RunAll()
    {
        await ParseAll();
        foreach (var fixture in ProcLibStopCases.Modules)
        {
            Term actual = await RunFixture(fixture);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                fixture.Name,
                Expected = fixture.Expected.ToString(),
                Actual = actual.ToString(),
                Passed = true
            }));
        }
    }

    public static async Task RunDirect()
    {
        foreach (var item in new[] { (ProcLibStopCaseNames.Module, ProcLibStopCaseNames.Stop, 1), (ProcLibStopCaseNames.Module, ProcLibStopCaseNames.Stop, 3), (ProcLibStopCaseNames.Sys, ProcLibStopCaseNames.Terminate, 2), (ProcLibStopCaseNames.Sys, ProcLibStopCaseNames.Terminate, 3) })
        {
            await Run(item.Item1, item.Item2, item.Item3);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { Module = item.Item1, Function = item.Item2, Arity = item.Item3, Direct = true, Passed = true }));
        }
    }

    public static async Task Run(string module, string function, int arity)
    {
        string name = (module, function, arity) switch
        {
            (ProcLibStopCaseNames.Module, ProcLibStopCaseNames.Stop, 1) => ProcLibStopCaseNames.DefaultNormal,
            (ProcLibStopCaseNames.Module, ProcLibStopCaseNames.Stop, 3) => ProcLibStopCaseNames.ExactReason,
            (ProcLibStopCaseNames.Sys, ProcLibStopCaseNames.Terminate, 2) => ProcLibStopCaseNames.SysMissingName,
            (ProcLibStopCaseNames.Sys, ProcLibStopCaseNames.Terminate, 3) => ProcLibStopCaseNames.SysReturnsBeforeDeath,
            _ => throw new InvalidOperationException(ProcLibStopCaseNames.Mismatch)
        };

        await RunFixture(ProcLibStopCases.Modules.Single(value => value.Name == name));
    }

    private static async Task<Term> RunFixture(CompiledModuleCase fixture)
    {
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        Term actual = await CompiledModuleExecution.Run(artifact);
        if (!actual.Equals(fixture.Expected))
            throw new InvalidOperationException($"{ProcLibStopCaseNames.Mismatch}: {fixture.Name}; expected={fixture.Expected}; actual={actual}");

        return actual;
    }
}
