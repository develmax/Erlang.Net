namespace Erlang.Differential;

public static class RuntimeReductionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            RuntimeReductionCaseNames.Monotonic,
            RuntimeReductionCaseSources.Monotonic,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            RuntimeReductionCaseNames.Independent,
            RuntimeReductionCaseSources.Independent,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            RuntimeReductionCaseNames.Retired,
            RuntimeReductionCaseSources.Retired,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
