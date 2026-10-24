namespace Erlang.Tests;

internal static class MapFunKeyOrderHostChecks
{
    internal static Task Run()
    {
        FunctionTerm first = Function(MapFunKeyOrderHostNames.ReversedFirstUnique, MapFunKeyOrderHostNames.FirstOldUnique);
        FunctionTerm second = Function(MapFunKeyOrderHostNames.ReversedSecondUnique, MapFunKeyOrderHostNames.SecondOldUnique);
        var left = new MapTerm(new Dictionary<Term, Term>
        {
            [first] = Term.I(0),
            [second] = Term.I(1)
        });
        var right = new MapTerm(new Dictionary<Term, Term>
        {
            [first] = Term.I(1),
            [second] = Term.I(0)
        });
        Require(first.CompareTo(second) < 0 && !first.Equals(second));
        Require(left.Entries.Count == 2 && right.Entries.Count == 2);
        Require(left.Get(first).Equals(Term.I(0)) && left.Get(second).Equals(Term.I(1)));
        Require(left.CompareTo(right) < 0 && right.CompareTo(left) > 0);
        Require(!left.Equals(right) && !left.NumericEquals(right));
        FunctionTerm equalOrderDistinctIdentity = Function(MapFunKeyOrderHostNames.TiedDistinctUnique, MapFunKeyOrderHostNames.FirstOldUnique);
        Require(first.CompareTo(equalOrderDistinctIdentity) == 0 && !first.Equals(equalOrderDistinctIdentity));
        var distinct = new MapTerm(new Dictionary<Term, Term>
        {
            [first] = Term.I(0),
            [equalOrderDistinctIdentity] = Term.I(1)
        });
        Require(distinct.Entries.Count == 2);
        Require(distinct.Get(first).Equals(Term.I(0)) && distinct.Get(equalOrderDistinctIdentity).Equals(Term.I(1)));
        Term deepLeft = left;
        Term deepRight = right;
        Term firstKey = first;
        Term secondKey = second;
        for (int depth = 0; depth < MapFunKeyOrderHostNames.DeepWrappers; depth++)
        {
            deepLeft = Term.Tuple(deepLeft);
            deepRight = Term.Tuple(deepRight);
            firstKey = Term.Tuple(firstKey);
            secondKey = Term.Tuple(secondKey);
        }
        Require(deepLeft.CompareTo(deepRight) < 0 && deepRight.CompareTo(deepLeft) > 0);
        Require(!deepLeft.Equals(deepRight));
        var deepKeysLeft = new MapTerm(new Dictionary<Term, Term>
        {
            [firstKey] = Term.I(0),
            [secondKey] = Term.I(1)
        });
        var deepKeysRight = new MapTerm(new Dictionary<Term, Term>
        {
            [firstKey] = Term.I(1),
            [secondKey] = Term.I(0)
        });
        Require(deepKeysLeft.CompareTo(deepKeysRight) < 0 && deepKeysRight.CompareTo(deepKeysLeft) > 0);
        Require(left.CompareTo(right) < 0);

        return Task.CompletedTask;
    }

    private static FunctionTerm Function(byte unique, int oldUnique) => new(
        new LocalFunctionIdentity(
            Term.A(MapFunKeyOrderHostNames.Module),
            null,
            0,
            oldUnique,
            new BitString(Enumerable.Repeat(unique, MapFunKeyOrderHostNames.FunctionUniqueBytes).ToArray())
        ),
        0,
        [],
        (_, _) => ValueTask.FromResult<Term>(Nil.Value)
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(MapFunKeyOrderHostNames.Failure);
    }
}
