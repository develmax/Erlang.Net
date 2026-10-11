namespace Erlang.Differential;

public static class SpawnContextDiagnosticCases
{
    public static IReadOnlyList<CompiledModuleCase> All { get; } = ProcessSpawnCases.Modules.Where(fixture => fixture.Name is
        "erlang-spawn-group-mfa-linked" or
        "erlang-spawn-group-mfa-link-child-error" or
        "erlang-spawn-group-callback-tuple" or
        "erlang-spawn-group-callback-wrong-arity-child-error").Append(new(
            "native-fresh-child-initial-trap-exit",
            "-module(oracle_fresh_context). -export([run/0]). run()->process_flag(trap_exit,true).",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(ErlangBooleanAtoms.False))
        )).ToArray();

    public static async Task<IReadOnlyList<object>> Run(Func<string, Task<string>> execute)
    {
        var results = new List<object>();
        foreach (CompiledModuleCase fixture in All)
        {
            string initialOutput = await execute(CompiledModuleProtocol.InitialProcessCommand(fixture.Source));
            string freshOutput = await execute(CompiledModuleProtocol.Command(fixture.Source));
            Term initial = OracleResultProtocol.Decode(initialOutput);
            Term fresh = OracleResultProtocol.Decode(freshOutput);
            results.Add(new
            {
                fixture.Name,
                fixture.Source,
                FixtureExpected = fixture.Expected.ToString(),
                InitialProcessOutcome = initial.ToString(),
                FreshProcessOutcome = fresh.ToString(),
                FreshMatchesIndependentExpected = fresh.Equals(fixture.Expected),
                InitialOutput = initialOutput,
                FreshOutput = freshOutput,
                Scope = "Separate context diagnosis; not counted as additional expression/module corpus coverage"
            });
        }

        return results;
    }
}
