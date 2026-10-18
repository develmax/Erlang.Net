namespace Erlang.Tests;

internal static class ProplistOwnCursorAssertions
{
    internal static void Require(bool condition, string detail)
    {
        if (!condition)
            throw new InvalidOperationException(ProplistOwnCursorDiagnostics.Failure + detail);
    }

    internal static MapTerm WithKey(Term key) => new(
        Enumerable.Range(0, ProplistOwnCursorNames.ScalarKeys)
            .Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.I(index)))
            .Append(new(key, key)));

    internal static async Task<Term> Read(ProcessContext context, Term input) => await context.Runtime.Modules.Call(
        context,
        ProplistOwnCursorNames.Proplists,
        ProplistOwnCursorNames.FromMap,
        input
    );

    internal static void SameAssociations(Term actual, IEnumerable<KeyValuePair<Term, Term>> expected)
    {
        Term[] wanted = expected.Select(pair => Term.Tuple(pair.Key, pair.Value)).ToArray();
        Term[] returned = Cons.Items(actual).ToArray();
        Require(
            returned.All(term => term is TupleTerm { Items.Count: 2 })
            && returned.Length == wanted.Length
            && returned.Distinct().Count() == returned.Length
            && returned.ToHashSet().SetEquals(wanted),
            ProplistOwnCursorDiagnostics.OutputShape
        );
    }

    internal static async Task BadMap(ProcessContext context, Term input)
    {
        try
        {
            await Read(context, input);
        }
        catch (ErlangException error)
        {
            Term location = Term.List(
                Term.Tuple(Term.A(ProplistIteratorTestNames.SourceFile), Term.String(ProplistIteratorTestNames.MapsFile)),
                Term.Tuple(Term.A(ProplistIteratorTestNames.SourceLine), Term.I(ProplistIteratorTestNames.MapsLine)),
                Term.Tuple(
                    Term.A(ProplistIteratorTestNames.ErrorInfo),
                    new MapTerm(
                    new Dictionary<Term, Term>
                    {
                        [Term.A(ProplistIteratorTestNames.ErrorModule)] = Term.A(ProplistIteratorTestNames.ErrorFormatter)
                    })
                )
            );
            Term frame = Term.Tuple(
                Term.A(ProplistIteratorTestNames.Maps),
                Term.A(ProplistIteratorTestNames.ToList),
                Term.List(input),
                location
            );
            Require(
                error.ExceptionClass == ProplistOwnCursorNames.ErrorClass
                && error.Reason.Equals(Term.Tuple(Term.A(ProplistOwnCursorNames.BadMap), input))
                && error.StackTraceTerm.Equals(Term.List(frame)),
                ProplistOwnCursorDiagnostics.Epochs
            );

            return;
        }

        throw new InvalidOperationException(ProplistOwnCursorDiagnostics.Epochs);
    }

    internal static async Task<Term> RootSuffix(ProcessContext context, MapTerm map, ProplistOwnHashScope scope)
    {
        var partitions = map.Entries.Select(pair => (Pair: pair, Nibble: scope.Hash(pair.Key) & ProplistOwnCursorNames.RootNibbleMask)).ToArray();
        Require(partitions.Select(item => item.Nibble).Distinct().Count() > 1, ProplistOwnCursorDiagnostics.RootPartition);
        ulong first = partitions.Min(item => item.Nibble);
        Term cursor = new Cons(Term.I(ProplistOwnCursorNames.RootSlot), map);
        Term result = await Read(context, cursor);
        SameAssociations(result, partitions.Where(item => item.Nibble > first).Select(item => item.Pair));
        Term again = await Read(context, cursor);
        Require(result.Equals(again), ProplistOwnCursorDiagnostics.Stable);
        SameAssociations(await Read(context, map), map.Entries);

        return result;
    }
}
