using System.Numerics;
namespace Erlang;

internal static class AllocatorGatherTerms434
{
    private static Term Number(ulong value) => new Integer(new BigInteger(value));

    private static TupleTerm Histogram(ulong[] slots) => Term.Tuple(slots.Select(Number).ToArray());

    internal static Term Histograms(AllocatorGatherRequest434 request, IReadOnlyList<AllocatorBlockWindow434> blocks)
    {
        var groups = new Dictionary<(Term Identity, string Type), ulong[]>();
        foreach (var block in blocks)
        {
            var key = (block.Identity, block.Type);
            if (!groups.TryGetValue(key, out var slots))
            {
                slots = new ulong[request.Width];
                groups.Add(key, slots);
            }
            if (slots.Length == 0) continue;
            ulong relative = block.Bytes / request.Start;
            int bucket = relative == 0 ? 0 : 64 - BitOperations.LeadingZeroCount(relative);
            bucket = Math.Min(bucket, slots.Length - 1);
            slots[bucket] = checked(slots[bucket] + 1);
        }
        Term rows = Cons.From(groups.OrderBy(row => row.Key.Identity, Comparer<Term>.Create((a, b) => TermOrder.Compare(a, b))).ThenBy(row => row.Key.Type, StringComparer.Ordinal)
            .Select(row => Term.Tuple(row.Key.Identity, Term.A(row.Key.Type), Histogram(row.Value))));

        return Term.Tuple(request.Reference!, Term.I(0), rows);
    }

    internal static Term Carriers(AllocatorGatherRequest434 request, IReadOnlyList<AllocatorBlockWindow434> blocks)
    {
        Term rows = Cons.From(blocks.Select(block => Term.Tuple(
            Term.A(block.Allocator),
            Term.A(ErlangBooleanAtoms.False),
            Number(block.Bytes),
            Term.I(0),
            Term.List(Term.Tuple(Term.A(block.Allocator), Term.I(1), Number(block.Bytes))),
            Histogram(new ulong[request.Width])
        )));

        return Term.Tuple(request.Reference!, rows);
    }
}
