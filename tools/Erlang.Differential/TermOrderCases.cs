namespace Erlang.Differential;

public static class TermOrderCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            TermOrderCaseNames.DeepFunEnvironment,
            TermOrderCaseSources.DeepFunEnvironment,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(TermOrderCaseNames.Done))
        ),
        new(
            TermOrderCaseNames.DeepListHead,
            TermOrderCaseSources.DeepListHead,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(TermOrderCaseNames.Done))
        ),
        new(
            TermOrderCaseNames.MapKeyPhases,
            TermOrderCaseSources.MapKeyPhases,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(TermOrderCaseNames.Done))
        ),
        new(
            TermOrderCaseNames.DeepMapValue,
            TermOrderCaseSources.DeepMapValue,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(TermOrderCaseNames.Done))
        ),
        new(
            TermOrderCaseNames.DeepTuple,
            TermOrderCaseSources.DeepTuple,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(TermOrderCaseNames.Done))
        )
    ];
}
