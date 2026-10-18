namespace Erlang;

internal static class BinarySearchResults
{
    internal static Term Position(BinarySearchHit hit) => Term.Tuple(Term.I(hit.Position), Term.I(hit.Length));

    internal static async ValueTask<Term> Split(ProcessContext context, BitString subject, IReadOnlyList<BinarySearchHit> hits, BinarySearchOptions options)
    {
        await context.ChargeAsync(0);
        if (hits.Count == 0)
            return subject.BitLength == 0 && (options.Trim || options.TrimAll) ? Nil.Value : Term.List(subject);
        var work = new BinarySearchWork(context);
        var ranges = new List<BinarySearchHit>(hits.Count + 1);
        int previous = 0;
        foreach (BinarySearchHit hit in hits)
        {
            await work.Step();
            ranges.Add(new(previous, hit.Position - previous));
            previous = hit.Position + hit.Length;
        }
        ranges.Add(new(previous, subject.ByteLength - previous));
        if (options.Trim)
            while (ranges.Count > 0 && ranges[^1].Length == 0)
            {
                await work.Step();
                ranges.RemoveAt(ranges.Count - 1);
            }
        var parts = new List<Term>(ranges.Count);
        foreach (BinarySearchHit range in ranges)
        {
            await work.Step();
            if (options.TrimAll && range.Length == 0)
                continue;
            parts.Add(subject.SliceBits(checked(range.Position * 8), checked(range.Length * 8)));
        }
        return await List(context, parts);
    }
    internal static async ValueTask<Term> List(ProcessContext context, IReadOnlyList<Term> values)
    {
        var work = new BinarySearchWork(context);
        Term result = Nil.Value;
        for (int i = values.Count - 1; i >= 0; i--)
        {
            await work.Step();
            result = new Cons(values[i], result);
        }
        return result;
    }
}
