namespace Erlang.Differential;

public static class AsyncDistributionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        Term yes = Term.A(AsyncDistributionCaseNames.True);
        Term no = Term.A(AsyncDistributionCaseNames.False);

        CompiledModuleCase Case(string name, string body, Term result) => new(
            AsyncDistributionCaseNames.Prefix + name,
            AsyncDistributionCaseNames.ModulePrefix + body + AsyncDistributionCaseNames.ModuleSuffix,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), result)
        );

        return [
            Case(AsyncDistributionCaseNames.DefaultName, AsyncDistributionCaseNames.DefaultBody, no),
            Case(AsyncDistributionCaseNames.TransitionsName, AsyncDistributionCaseNames.TransitionsBody, Term.Tuple(
                no,
                yes,
                yes,
                no
            )),
            new(
                AsyncDistributionCaseNames.Prefix + AsyncDistributionCaseNames.IntegerName,
                AsyncDistributionCaseNames.ModulePrefix + AsyncDistributionCaseNames.IntegerBody + AsyncDistributionCaseNames.ModuleSuffix,
                Term.Tuple(
                    Term.A(OracleOutcomeTags.Failure),
                    Term.A(AsyncDistributionCaseNames.Error),
                    Term.A(AsyncDistributionCaseNames.BadArgument)
                )
            ),
            Case(AsyncDistributionCaseNames.AtomName, AsyncDistributionCaseNames.AtomBody, yes),
            Case(AsyncDistributionCaseNames.ChildName, AsyncDistributionCaseNames.ChildBody, Term.Tuple(no, yes))
        ];
    }
}
