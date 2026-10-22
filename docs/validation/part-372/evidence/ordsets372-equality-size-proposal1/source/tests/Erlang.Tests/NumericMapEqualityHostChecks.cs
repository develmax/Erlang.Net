namespace Erlang.Tests;

internal static class NumericMapEqualityHostChecks
{
    internal static void Run()
    {
        FunctionTerm Local(byte unique, int oldUnique) => new(
            new LocalFunctionIdentity(
                Term.A(NumericMapEqualityHostNames.Module),
                null,
                0,
                oldUnique,
                new BitString(Enumerable.Repeat(unique, NumericMapEqualityHostNames.UniqueBytes).ToArray())
            ),
            0,
            Array.Empty<Term>(),
            (_, _) => ValueTask.FromResult<Term>(Term.I(0))
        );

        Term a = Local(byte.MinValue, NumericMapEqualityHostNames.FirstOldUnique);
        Term b = Local(byte.MaxValue, NumericMapEqualityHostNames.SecondOldUnique);
        Term c = Local(NumericMapEqualityHostNames.ThirdUniqueByte, NumericMapEqualityHostNames.SecondOldUnique);
        Term d = Local(NumericMapEqualityHostNames.FourthUniqueByte, NumericMapEqualityHostNames.FirstOldUnique);

        MapTerm Map(params (Term Key, Term Value)[] entries) => new(
            entries.Select(entry => new KeyValuePair<Term, Term>(entry.Key, entry.Value))
        );

        void Equal(bool expected, Term left, Term right)
        {
            if (left.NumericEquals(right) != expected || right.NumericEquals(left) != expected)
                throw new InvalidOperationException(NumericMapEqualityHostNames.Failure);
        }

        Term first = Map((a, Term.I(0)), (b, Term.I(1)));
        Term second = Map((c, Term.I(1)), (d, Term.I(0)));
        Equal(true, first, second);
        Equal(true, Term.List(first), Term.List(second));
        Equal(true, Term.Tuple(first, Term.I(1)), Term.Tuple(second, new FloatTerm(1)));
        Equal(false, first, Map((c, Term.I(0)), (d, Term.I(1))));

        Term ordinaryKey = Term.A(NumericMapEqualityHostNames.OrdinaryKey);
        Term otherKey = Term.A(NumericMapEqualityHostNames.OtherKey);
        Term integerValueMap = Map((ordinaryKey, Term.I(1)), (otherKey, Term.I(2)));
        Term floatValueMap = Map((ordinaryKey, new FloatTerm(1)), (otherKey, new FloatTerm(2)));
        Equal(true, integerValueMap, floatValueMap);
        Equal(false, Map((integerValueMap, Term.I(0))), Map((floatValueMap, Term.I(0))));

        // Numeric local-fun comparison deliberately differs from exact code identity.
        Equal(true, a, d);
        if (a.Equals(d))
            throw new InvalidOperationException(NumericMapEqualityHostNames.Failure);
    }
}
