namespace Erlang.Differential;

public static class FullSweepCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            FullSweepCaseNames.FullsweepProcessDomain,
            FullSweepCaseSources.FullsweepProcessDomain,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(FullSweepCaseNames.Done))
        ),
        new(
            FullSweepCaseNames.FullsweepSpawnForms,
            FullSweepCaseSources.FullsweepSpawnForms,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(FullSweepCaseNames.Done))
        ),
        new(
            FullSweepCaseNames.FullsweepRequestForms,
            FullSweepCaseSources.FullsweepRequestForms,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(FullSweepCaseNames.Done))
        ),
        new(
            FullSweepCaseNames.FullsweepDefaultSnapshot,
            FullSweepCaseSources.FullsweepDefaultSnapshot,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(FullSweepCaseNames.Done))
        )
    ];
}
