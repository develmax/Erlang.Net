namespace Erlang.Tests;

// Synthetic coverage-predicate controls only; no expected runtime values or
// original golden is manufactured here.
internal static class OriginalAtomics588CaseChecks
{
    internal static Task HiddenFailureAndMissingCoverage()
    {
        Term success = Build(false, false, false);
        Require(OriginalAtomics588Cases.HasSuccessfulCases(success));
        Require(!OriginalAtomics588Cases.HasSuccessfulCases(Build(true, false, false)));
        Require(!OriginalAtomics588Cases.HasSuccessfulCases(Build(false, true, false)));
        Require(!OriginalAtomics588Cases.HasSuccessfulCases(Build(false, false, true)));

        return Task.CompletedTask;
    }

    private static Term Build(bool failedExercise, bool missingStack, bool duplicateCounter)
    {
        Term returned = Term.Tuple(Term.A(OriginalAtomics588CaseNames.Returned), Nil.Value);
        Term raised = Term.Tuple(
            Term.A(OriginalAtomics588CaseNames.Raised),
            Term.A(ErlangExceptionClasses.Error),
            Term.A(ErlangErrorReasons.BadArgument),
            Term.List(Term.A(OriginalAtomics588CaseNames.SyntheticStack))
        );

        Term Errors(string[] names) => Cons.From(names.Select((name, index) => Term.Tuple(
            Term.A(name),
            missingStack && index == 0 ? Term.Tuple(
                Term.A(OriginalAtomics588CaseNames.Raised),
                Term.A(ErlangExceptionClasses.Error),
                Term.A(ErlangErrorReasons.BadArgument),
                Nil.Value
            ) : raised
        )));
        var rows = from count in OriginalAtomics588CaseNames.ValidLengths
                   from signed in new[] { ErlangBooleanAtoms.True, ErlangBooleanAtoms.False }
                   select Term.Tuple(
                       Term.I(count),
                       Term.A(signed),
                       failedExercise && count == OriginalAtomics588CaseNames.FailedFixtureLength && signed == ErlangBooleanAtoms.True ? raised : returned
                   );
        Term fields = Term.List(
            Term.Tuple(Term.A(OriginalAtomics588CaseNames.WordSize), Term.I(OriginalAtomics588CaseNames.FixtureWordBytes)),
            Term.Tuple(Term.A(OriginalAtomics588CaseNames.AtomicsAllSeven), Cons.From(rows)),
            Term.Tuple(
                Term.A(OriginalAtomics588CaseNames.ConstructorErrors),
                Errors([OriginalAtomics588CaseNames.Zero, OriginalAtomics588CaseNames.Negative, OriginalAtomics588CaseNames.WrongSize, OriginalAtomics588CaseNames.WrongOptions, OriginalAtomics588CaseNames.ImproperOptions, OriginalAtomics588CaseNames.WrongSigned, OriginalAtomics588CaseNames.UnknownOption])
            ),
            Term.Tuple(
                Term.A(OriginalAtomics588CaseNames.NativeErrors),
                Errors([OriginalAtomics588CaseNames.GetIndexZero, OriginalAtomics588CaseNames.PutIndexPastEnd, OriginalAtomics588CaseNames.AddWrongIncrement, OriginalAtomics588CaseNames.AddGetWrongIndex, OriginalAtomics588CaseNames.ExchangeWrongValue, OriginalAtomics588CaseNames.CompareWrongValue, OriginalAtomics588CaseNames.InfoWrongReference])
            ),
            Term.Tuple(
                Term.A(OriginalAtomics588CaseNames.CountersDefaultAndAtomics),
                Term.List(
                    Term.Tuple(Nil.Value, returned),
                    Term.Tuple(duplicateCounter ? Nil.Value : Term.List(Term.A(OriginalAtomics588CaseNames.AtomicsOption)), returned)
                )
            )
        );

        return Term.Tuple(Term.Tuple(Term.A(OriginalAtomics588CaseNames.Returned), fields), Term.A(OriginalAtomics588CaseNames.Normal));
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(OriginalAtomics588Names.Failure);
    }
}
