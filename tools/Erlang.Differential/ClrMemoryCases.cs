namespace Erlang.Differential;

public static class ClrMemoryCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(
            string name,
            string body,
            Term expected
        )
        {
            cases.Add(new(
                ClrMemoryCaseNames.Prefix + name,
                ClrMemoryCaseNames.ModulePrefix + body + ClrMemoryCaseNames.ModuleSuffix,
                expected
            ));
        }

        Term Failure(string reason) => Term.Tuple(
            Term.A(OracleOutcomeTags.Failure),
            Term.A(ClrMemoryCaseNames.ErrorClass),
            Term.A(reason)
        );

        Add(
            ClrMemoryCaseNames.AllShapeName,
            ClrMemoryCaseNames.AllShapeBody,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ClrMemoryCaseNames.True))
        );
        Add(
            ClrMemoryCaseNames.ScalarName,
            ClrMemoryCaseNames.ScalarBody,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ClrMemoryCaseNames.True))
        );
        Add(
            ClrMemoryCaseNames.OrderDuplicatesName,
            ClrMemoryCaseNames.OrderDuplicatesBody,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ClrMemoryCaseNames.True))
        );
        Add(
            ClrMemoryCaseNames.EmptyName,
            ClrMemoryCaseNames.EmptyBody,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Nil.Value)
        );
        Add(
            ClrMemoryCaseNames.WrongOuterName,
            ClrMemoryCaseNames.WrongOuterBody,
            Failure(ClrMemoryCaseNames.BadArgument)
        );
        Add(
            ClrMemoryCaseNames.UnknownName,
            ClrMemoryCaseNames.UnknownBody,
            Failure(ClrMemoryCaseNames.BadArgument)
        );
        Add(
            ClrMemoryCaseNames.MaximumName,
            ClrMemoryCaseNames.MaximumBody,
            Failure(ClrMemoryCaseNames.BadArgument)
        );
        Add(
            ClrMemoryCaseNames.ValidImproperName,
            ClrMemoryCaseNames.ValidImproperBody,
            Failure(ClrMemoryCaseNames.FunctionClause)
        );
        Add(
            ClrMemoryCaseNames.InvalidImproperName,
            ClrMemoryCaseNames.InvalidImproperBody,
            Failure(ClrMemoryCaseNames.BadArgument)
        );
        Add(
            ClrMemoryCaseNames.NonatomName,
            ClrMemoryCaseNames.NonatomBody,
            Failure(ClrMemoryCaseNames.BadArgument)
        );

        return cases;
    }
}
