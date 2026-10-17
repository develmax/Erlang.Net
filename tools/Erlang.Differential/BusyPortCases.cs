namespace Erlang.Differential;

public static class BusyPortCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            BusyPortCaseNames.Case0,
            BusyPortCaseSources.Case0,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            BusyPortCaseNames.Case1,
            BusyPortCaseSources.Case1,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            BusyPortCaseNames.Case2,
            BusyPortCaseSources.Case2,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            BusyPortCaseNames.Case3,
            BusyPortCaseSources.Case3,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            BusyPortCaseNames.Case4,
            BusyPortCaseSources.Case4,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            BusyPortCaseNames.Case5,
            BusyPortCaseSources.Case5,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            BusyPortCaseNames.Case6,
            BusyPortCaseSources.Case6,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            BusyPortCaseNames.Case7,
            BusyPortCaseSources.Case7,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
