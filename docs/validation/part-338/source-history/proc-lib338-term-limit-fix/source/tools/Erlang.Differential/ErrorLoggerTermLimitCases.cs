namespace Erlang.Differential;

public static class ErrorLoggerTermLimitCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = ErrorLoggerTermLimitCaseData.Rows.Select(row => new CompiledModuleCase(
        ProcLibLifecycleCaseNames.Prefix + row.Name,
        row.Source,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibLifecycleCaseNames.Ok))
    )).ToArray();
}
