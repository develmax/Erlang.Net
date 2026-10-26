namespace Erlang.Differential;

public static class ProcLibLifecycleCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = ProcLibLifecycleCaseData.Asserted.Select(row => new CompiledModuleCase(
        ProcLibLifecycleCaseNames.Prefix + row.Name,
        row.Source,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibLifecycleCaseNames.Ok))
    )).ToArray();
}
