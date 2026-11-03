using System.Numerics;

namespace Erlang;

internal sealed class RuntimeNumericMapLayout(ProcessRuntime runtime) : ITermMapOrderingLayout
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

    public int CompareHashKeys(Term left, Term right)
    {
        using var hashes = runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);
        ulong first = hashes.Hash(left);
        ulong second = hashes.Hash(right);
        for (int level = 0; level < HamtViewLayout.HashLevels; level++)
        {
            int order = (first & HamtViewLayout.NibbleMask).CompareTo(second & HamtViewLayout.NibbleMask);
            if (order != 0)
                return order;
            first >>= HamtViewLayout.NibbleBits;
            second >>= HamtViewLayout.NibbleBits;
        }

        return 0;
    }

    public NativeMapEqualityPlan505? ReadExactEqualityPlan(MapTerm left, MapTerm right)
    {
        if (NativeMapMaterialization505.TryRead(left, runtime, out var first))
            return first.EqualityPlan(right);
        if (NativeMapMaterialization505.TryRead(right, runtime, out var second))
            return second.EqualityPlan(left);

        return null;
    }

    public void Dispose() => layouts.Clear();
}
