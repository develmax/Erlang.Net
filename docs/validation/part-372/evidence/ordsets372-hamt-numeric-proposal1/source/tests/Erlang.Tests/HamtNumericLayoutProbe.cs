namespace Erlang.Tests;

internal sealed class HamtNumericLayoutProbe(bool cancel) : ITermMapComparisonLayout
{
    internal int ReadCount { get; private set; }
    internal int DisposeCount { get; private set; }

    public IReadOnlyList<KeyValuePair<Term, Term>> ReadHashEntries(MapTerm map)
    {
        ReadCount++;
        if (cancel)
            throw new OperationCanceledException();

        return map.Entries;
    }

    public void Dispose() => DisposeCount++;
}
