namespace Erlang.Tests;

internal static class ProplistFlatCursorExpectation
{
    internal static Term ForProfile(string file, Term literalExpected)
    {
        if (file != ProplistFlatIteratorTestNames.File)
            return literalExpected;
        // OTP flat-map atom keys use the current VM atom indices. Keep literal
        // values exact and order only this fixture's expected rows, never output.
        TupleTerm[] entries = Cons.Items(literalExpected).Select(value => value is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom
            ? tuple : throw new InvalidOperationException(ProplistFamilyTestNames.Failure)).ToArray();

        return Cons.From(entries.OrderBy(entry => AtomNameRegistry.IndexOf(((Atom)entry.Items[0]).Name)));
    }
}
