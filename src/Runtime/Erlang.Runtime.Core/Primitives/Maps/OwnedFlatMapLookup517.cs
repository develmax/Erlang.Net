namespace Erlang;

internal sealed class OwnedFlatMapLookup517(IReadOnlyList<KeyValuePair<Term, Term>> rows)
{
    internal (bool Found, Term? Value) Find(Term key)
    {
        // Same first exact-equal occurrence as the existing native flat shadow.
        // Each payload read releases the heap gate before term equality runs.
        foreach (var row in rows)
            if (row.Key.Equals(key))
                return (true, row.Value);

        return (false, null);
    }
}
