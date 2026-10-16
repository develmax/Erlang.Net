using Erlang.Differential;

namespace Erlang.Tests;

public static class ProcLibMfaContracts
{
    public static bool Owns(string module) => module == ProcLibCaseNames.Module;

    public static async Task Run(string function, int arity)
    {
        string name = (function, arity) switch
        {
            (ProcLibCaseNames.Spawn, 1) => ProcLibCaseNames.FunSpawn,
            (ProcLibCaseNames.Spawn, 3) => ProcLibCaseNames.MfaSpawn,
            (ProcLibCaseNames.SpawnLink, 1) => ProcLibCaseNames.FunLink,
            (ProcLibCaseNames.SpawnLink, 3) => ProcLibCaseNames.MfaLink,
            (ProcLibCaseNames.InitAck, 1) => ProcLibCaseNames.AckOne,
            (ProcLibCaseNames.InitAck, 2) => ProcLibCaseNames.AckTwo,
            _ => throw new InvalidOperationException(ProcLibCaseNames.MissingContract)
        };
        var fixture = ProcLibCases.Modules.Single(value => value.Name == name);
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        Term actual = await CompiledModuleExecution.Run(artifact);
        if (!actual.Equals(fixture.Expected))
            throw new InvalidOperationException(ProcLibCaseNames.Mismatch);
    }
}
