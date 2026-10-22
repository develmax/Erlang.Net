using System.Numerics;

namespace Erlang;

internal sealed class RuntimeNumericMapLayout(ProcessRuntime runtime) : ITermMapComparisonLayout
{
    private readonly Dictionary<MapTerm, IReadOnlyList<KeyValuePair<Term, Term>>> layouts = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<KeyValuePair<Term, Term>> ReadHashEntries(MapTerm map)
    {
        if (layouts.TryGetValue(map, out var known))
            return known;
        using var index = runtime.AcquireHamtIndex429(map);
        Term cursor = HamtCursorView.ReadIntegerCursor(
            map,
            BigInteger.Zero,
            null!,
            index.Index.Root
        );
        var result = Cons.Items(cursor)
            .Cast<TupleTerm>()
            .Select(pair => new KeyValuePair<Term, Term>(pair.Items[0], pair.Items[1]))
            .ToArray();
        layouts.Add(map, result);

        return result;
    }

    public void Dispose() => layouts.Clear();
}
