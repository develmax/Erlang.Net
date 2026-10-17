namespace Erlang.Differential;

public static class SchedulerStatisticsCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            SchedulerStatisticsCaseNames.Shape,
            SchedulerStatisticsCaseSources.Shape,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
