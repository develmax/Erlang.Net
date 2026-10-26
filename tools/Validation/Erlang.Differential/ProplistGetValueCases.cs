namespace Erlang.Differential;

public static class ProplistGetValueCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = ProplistGetValueCaseSources.Rows
        .Select(row => new CompiledModuleCase(
            ProplistGetValueCaseNames.Prefix + row.Name,
            ProplistGetValueCaseNames.Header + row.Body + ProplistGetValueCaseNames.Suffix,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProplistGetValueCaseNames.Completion))
        ))
        .ToArray();
}
