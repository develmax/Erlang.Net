namespace Erlang.Tests;

internal static class ProplistFlatExpectationHostChecks
{
    internal static Task Run()
    {
        string prefix = ProplistFlatExpectationNames.Prefix + Guid.NewGuid().ToString(ProplistFlatExpectationNames.Format);
        Atom second = Term.A(prefix + ProplistFlatExpectationNames.Second);
        Atom first = Term.A(prefix + ProplistFlatExpectationNames.First);
        Term firstPair = Term.Tuple(first, Term.I(1));
        Term secondPair = Term.Tuple(second, Term.I(2));
        Term literal = Term.List(firstPair, secondPair);
        Term expected = ProplistFlatCursorExpectation.ForProfile(ProplistFlatIteratorTestNames.File, literal);
        Require(AtomNameRegistry.IndexOf(second.Name) < AtomNameRegistry.IndexOf(first.Name));
        Require(expected.Equals(Term.List(secondPair, firstPair)));
        // Wrong order, changed value, missing row and duplicate row all stay unequal.
        foreach (Term changed in new Term[]
        {
            literal,
            Term.List(Term.Tuple(second, Term.I(3)), firstPair),
            Term.List(secondPair),
            Term.List(secondPair, secondPair)
        })
            Require(!expected.Equals(changed));
        Require(ReferenceEquals(ProplistFlatCursorExpectation.ForProfile(ProplistIteratorTestNames.File, literal), literal));

        return Task.CompletedTask;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProplistFamilyTestNames.Failure);
    }
}
