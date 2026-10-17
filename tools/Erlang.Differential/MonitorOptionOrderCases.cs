namespace Erlang.Differential;

public static class MonitorOptionOrderCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            MonitorOptionOrderCaseNames.Order,
            MonitorOptionOrderCaseSources.Order,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
