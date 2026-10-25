namespace Erlang;

public interface ITermMapComparisonLayout : IDisposable
{
    IReadOnlyList<KeyValuePair<Term, Term>> ReadHashEntries(MapTerm map);
}
