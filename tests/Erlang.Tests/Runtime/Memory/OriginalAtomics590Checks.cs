namespace Erlang.Tests;

// Synthetic finite comparator controls, never an original golden.
internal static class OriginalAtomics590Checks
{
    internal static Task AliasDomainAndStackRefusal()
    {
        ReferenceTerm a = new(OriginalAtomics590Names.LocalNode, OriginalAtomics590SyntheticNames.SourceWords);
        ReferenceTerm b = new(OriginalAtomics590Names.LocalNode, OriginalAtomics590SyntheticNames.TargetWords);
        ReferenceTerm c = new(OriginalAtomics590Names.LocalNode, OriginalAtomics590SyntheticNames.AlternateWords);
        byte[] source = Encode(Build(a));
        byte[] target = Encode(Build(b));
        var pair = OriginalAtomics590Identity.Compare(source, target);
        Require(pair.Passed && pair.OriginalOccurrences == OriginalAtomics590Names.ExpectedOwnOccurrences && pair.ManagedOccurrences == OriginalAtomics590Names.ExpectedOwnOccurrences);
        Require(!OriginalAtomics590Identity.Compare(source, Encode(Build(b, alias: c))).Passed);
        Require(!OriginalAtomics590Identity.Compare(source, Encode(Build(new ReferenceTerm(OriginalAtomics590SyntheticNames.ForeignNode, b.Words)))).Passed);
        Require(!OriginalAtomics590Identity.Compare(
            source,
            Encode(Build(new ReferenceTerm(OriginalAtomics590Names.LocalNode, b.Words, OriginalAtomics590SyntheticNames.ForeignCreation)))
        ).Passed);
        Require(!OriginalAtomics590Identity.Compare(
            source,
            Encode(Build(new ReferenceTerm(OriginalAtomics590Names.LocalNode, OriginalAtomics590SyntheticNames.AliasMarkedWords)))
        ).Passed);
        Require(!OriginalAtomics590Identity.Compare(source, Encode(Build(b, tail: OriginalAtomics590SyntheticNames.ChangedStack))).Passed);
        Require(!OriginalAtomics590Identity.Compare(Encode(Build(a, opaque: true)), Encode(Build(b, opaque: true))).Passed);
        Require(!OriginalAtomics590Identity.SameBinding(pair, OriginalAtomics590Identity.Compare(source, Encode(Build(c)))));

        return Task.CompletedTask;
    }

    private static byte[] Encode(Term value) => ExternalTermFormat.Encode(value);

    private static Term Build(
        ReferenceTerm reference,
        ReferenceTerm? alias = null,
        string tail = OriginalAtomics590SyntheticNames.SameStack,
        bool opaque = false
    )
    {
        string[] names = [OriginalAtomics588CaseNames.GetIndexZero, OriginalAtomics588CaseNames.PutIndexPastEnd, OriginalAtomics588CaseNames.AddWrongIncrement, OriginalAtomics588CaseNames.AddGetWrongIndex, OriginalAtomics588CaseNames.ExchangeWrongValue, OriginalAtomics588CaseNames.CompareWrongValue, OriginalAtomics588CaseNames.InfoWrongReference];
        string[] functions = [OriginalAtomics588Names.Get, OriginalAtomics588Names.Put, OriginalAtomics588Names.Add, OriginalAtomics588Names.AddGet, OriginalAtomics588Names.Exchange, OriginalAtomics588Names.CompareExchange, OriginalAtomics588Names.Info];
        Term[][] extras = [[Term.I(OriginalAtomics590Names.InvalidZeroIndex)],[Term.I(OriginalAtomics590Names.PastEndIndex),Term.I(OriginalAtomics590Names.ZeroReplacement)],[Term.I(OriginalAtomics590Names.ValidIndex),Term.A(OriginalAtomics590Names.BadArgumentAtom)],
            [Term.A(OriginalAtomics590Names.BadArgumentAtom),Term.I(OriginalAtomics590Names.ZeroReplacement)],[Term.I(OriginalAtomics590Names.ValidIndex),Term.A(OriginalAtomics590Names.BadArgumentAtom)],[Term.I(OriginalAtomics590Names.ValidIndex),Term.A(OriginalAtomics590Names.BadArgumentAtom),Term.I(OriginalAtomics590Names.ZeroReplacement)],[]];
        var errors = new List<Term>();
        for (int i = 0; i < OriginalAtomics590Names.ExpectedNativeErrors; i++)
        {
            Term first = i == OriginalAtomics590Names.InfoCaseIndex ? Term.A(OriginalAtomics590Names.BadArgumentAtom) : i == OriginalAtomics590SyntheticNames.AliasFailureCaseIndex && alias is not null ? alias : reference;
            Term args = Cons.From(new[] { first }.Concat(extras[i]));
            Term frame = Term.Tuple(
                Term.A(OriginalAtomics588Names.Atomics),
                Term.A(functions[i]),
                args,
                Term.List(Term.Tuple(
                    Term.A(OriginalAtomics590Names.ErrorInfo),
                    new MapTerm(new[] { new KeyValuePair<Term, Term>(Term.A(OriginalAtomics590Names.ErrorInfoModuleKey), Term.A(OriginalAtomics590Names.ErrorInfoModule)) })
                ))
            );
            Term stack = Term.List(
                frame,
                Term.Tuple(
                    Term.A(tail),
                    Term.A(OriginalAtomics590Names.WorkerFunction),
                    Term.I(OriginalAtomics590Names.WorkerArity),
                    Nil.Value
                )
            );
            errors.Add(Term.Tuple(
                Term.A(names[i]),
                Term.Tuple(
                    Term.A(OriginalAtomics588CaseNames.Raised),
                    Term.A(ErlangExceptionClasses.Error),
                    Term.A(ErlangErrorReasons.BadArgument),
                    stack
                )
            ));
        }
        Term fields = Term.List(
            Term.Tuple(Term.A(OriginalAtomics588CaseNames.NativeErrors), Cons.From(errors)),
            Term.Tuple(Term.A(OriginalAtomics590Names.OpaqueBytes), opaque ? new BitString(Encode(reference)) : Nil.Value)
        );

        return Term.Tuple(Term.Tuple(Term.A(OriginalAtomics588CaseNames.Returned), fields), Term.A(OriginalAtomics588CaseNames.Normal));
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(OriginalAtomics590SyntheticNames.Failure);
    }
}
