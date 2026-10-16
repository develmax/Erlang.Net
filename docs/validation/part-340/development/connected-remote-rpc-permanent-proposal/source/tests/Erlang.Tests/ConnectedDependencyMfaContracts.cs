using Erlang.Differential;

namespace Erlang.Tests;

public static class ConnectedDependencyMfaContracts
{
    public static bool Owns(string module, string function, int arity) => module switch
    {
        ConnectedDependencyCaseNames.ProcLib => function switch
        {
            ConnectedDependencyCaseNames.Spawn or ConnectedDependencyCaseNames.SpawnLink => arity is 2 or 4,
            ConnectedDependencyCaseNames.SpawnOpt => arity is 3 or 5,
            _ => false
        },
        ConnectedDependencyCaseNames.Rpc => function == ConnectedDependencyCaseNames.Call && arity is 4 or 5,
        ConnectedDependencyCaseNames.Erpc => function == ConnectedDependencyCaseNames.Call && arity is 4 or 5 ||
            function == ConnectedDependencyCaseNames.Execute && arity == 4,
        _ => false
    };

    public static Task ParseAll()
    {
        foreach (var fixture in ConnectedDependencyCases.Modules)
            new Erlang.Compiler.Parser(fixture.Source).ParseModule();

        return Task.CompletedTask;
    }

    public static async Task RunAll()
    {
        await ParseAll();
        foreach (var fixture in ConnectedDependencyCases.Modules)
            await Execute(fixture);
    }

    public static async Task Run(string module, string function, int arity)
    {
        string name = (module, function, arity) switch
        {
            (ConnectedDependencyCaseNames.ProcLib, ConnectedDependencyCaseNames.Spawn, 2) => ConnectedDependencyCaseNames.ProcNodeLocalSpawnFun,
            (ConnectedDependencyCaseNames.ProcLib, ConnectedDependencyCaseNames.Spawn, 4) => ConnectedDependencyCaseNames.ProcNodeLocalSpawnMfa,
            (ConnectedDependencyCaseNames.ProcLib, ConnectedDependencyCaseNames.SpawnLink, 2) => ConnectedDependencyCaseNames.ProcNodeLocalSpawnLinkFun,
            (ConnectedDependencyCaseNames.ProcLib, ConnectedDependencyCaseNames.SpawnLink, 4) => ConnectedDependencyCaseNames.ProcNodeLocalSpawnLinkMfa,
            (ConnectedDependencyCaseNames.ProcLib, ConnectedDependencyCaseNames.SpawnOpt, 3) => ConnectedDependencyCaseNames.ProcNodeLocalSpawnOptFun,
            (ConnectedDependencyCaseNames.ProcLib, ConnectedDependencyCaseNames.SpawnOpt, 5) => ConnectedDependencyCaseNames.ProcNodeLocalSpawnOptMfa,
            (ConnectedDependencyCaseNames.Rpc, ConnectedDependencyCaseNames.Call, 4) => ConnectedDependencyCaseNames.RpcLocalCaller,
            (ConnectedDependencyCaseNames.Rpc, ConnectedDependencyCaseNames.Call, 5) => ConnectedDependencyCaseNames.RpcZeroTimeoutRealChildCleanup,
            (ConnectedDependencyCaseNames.Erpc, ConnectedDependencyCaseNames.Call, 4) => ConnectedDependencyCaseNames.RpcLocalCaller,
            (ConnectedDependencyCaseNames.Erpc, ConnectedDependencyCaseNames.Call, 5) => ConnectedDependencyCaseNames.RpcFiniteUsesChild,
            (ConnectedDependencyCaseNames.Erpc, ConnectedDependencyCaseNames.Execute, 4) => ConnectedDependencyCaseNames.ErpcExecuteCallChildEnvelope,
            _ => throw new InvalidOperationException(ConnectedDependencyCaseNames.Mismatch)
        };
        await Execute(ConnectedDependencyCases.Modules.Single(fixture => fixture.Name == name));
    }

    private static async Task Execute(CompiledModuleCase fixture)
    {
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        Term actual = await CompiledModuleExecution.Run(artifact);
        bool passed = actual.Equals(fixture.Expected);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { fixture.Name, Expected = fixture.Expected.ToString(), Actual = actual.ToString(), Passed = passed }));
        if (!passed)
            throw new InvalidOperationException(ConnectedDependencyCaseNames.Mismatch);
    }
}
