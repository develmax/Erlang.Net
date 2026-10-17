namespace Erlang.Differential;

public static class NativeHostDefaultCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            NativeHostDefaultCaseNames.Case0,
            NativeHostDefaultCaseSources.Case0,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            NativeHostDefaultCaseNames.Case1,
            NativeHostDefaultCaseSources.Case1,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            NativeHostDefaultCaseNames.Case2,
            NativeHostDefaultCaseSources.Case2,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
