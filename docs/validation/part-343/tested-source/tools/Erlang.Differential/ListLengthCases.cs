namespace Erlang.Differential;

public static class ListLengthCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            ListLengthCaseNames.ComprehensionFilters,
            ListLengthCaseSources.ComprehensionFilters,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ListLengthCaseNames.Done))
        ),
        new(
            ListLengthCaseNames.DynamicPatterns,
            ListLengthCaseSources.DynamicPatterns,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ListLengthCaseNames.Done))
        ),
        new(
            ListLengthCaseNames.GuardContexts,
            ListLengthCaseSources.GuardContexts,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ListLengthCaseNames.Done))
        ),
        new(
            ListLengthCaseNames.LongBadarg,
            ListLengthCaseSources.LongBadarg,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ListLengthCaseNames.Done))
        ),
        new(
            ListLengthCaseNames.Matchspec,
            ListLengthCaseSources.Matchspec,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ListLengthCaseNames.Done))
        ),
        new(
            ListLengthCaseNames.ReceiveScan,
            ListLengthCaseSources.ReceiveScan,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ListLengthCaseNames.Done))
        ),
        new(
            ListLengthCaseNames.TryMaybe,
            ListLengthCaseSources.TryMaybe,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ListLengthCaseNames.Done))
        ),
        new(
            ListLengthCaseNames.Values,
            ListLengthCaseSources.Values,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ListLengthCaseNames.Done))
        ),
        new(
            ListLengthCaseNames.ZipDynamicSizes,
            ListLengthCaseSources.ZipDynamicSizes,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ListLengthCaseNames.Done))
        )
    ];
}
