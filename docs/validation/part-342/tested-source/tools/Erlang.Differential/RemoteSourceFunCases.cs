namespace Erlang.Differential;

public static class RemoteSourceFunCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            RemoteSourceFunCaseNames.LexicalRoundtrip,
            RemoteSourceFunCaseSources.LexicalRoundtrip,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(RemoteSourceFunCaseNames.SuccessAtom))
        )
    ];
}
