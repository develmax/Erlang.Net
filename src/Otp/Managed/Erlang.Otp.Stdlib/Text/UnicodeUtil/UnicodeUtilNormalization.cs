namespace Erlang;

internal static class UnicodeUtilNormalization
{
    private static UnicodeUtilData Data => UnicodeUtilData.Instance;

    internal static async ValueTask<Term> Normalize(ProcessContext context, string function, Term input)
    {
        Term fetched;
        using (UnicodeUtilSource.Scope(
            context,
            function,
            1,
            function == "nfd" ? UnicodeUtilSourceLines.NfdRead : function == "nfkd" ? UnicodeUtilSourceLines.NfkdRead : function == "nfc" ? UnicodeUtilSourceLines.NfcRead : UnicodeUtilSourceLines.NfkcRead
        ))
            fetched = await UnicodeUtilGraphemes.Gc(context, input);
        if (fetched is not Cons cell) return fetched;
        bool compatibility = function is "nfkd" or "nfkc";
        bool composition = function is "nfc" or "nfkc";
        if (UnicodeUtilTraversal.Codepoint(cell.Head, out int cp)
            && cp <= (function == "nfc" ? UnicodeUtilNames.Latin1Maximum : UnicodeUtilNames.AsciiMaximum)) return fetched;
        Term decomposed = await Decompose(context, cell.Head, compatibility);
        Term normalized = !composition ? decomposed : compatibility
            ? await ComposeCompatibility(context, decomposed) : await Compose(context, decomposed);

        return new Cons(normalized, cell.Tail);
    }

    private static async ValueTask<Term> Decompose(ProcessContext context, Term cluster, bool compatibility)
    {
        var tagged = new List<(int Class, int Cp)>();
        IEnumerable<Term> source = cluster is Integer ? [cluster] : Cons.Items(cluster);
        int work = 0;
        foreach (Term value in source)
        {
            int cp = (int)((Integer)value).Value;
            if (cp >= UnicodeUtilNames.HangulFirst && cp <= UnicodeUtilNames.HangulLast)
            {
                int syllable = cp - UnicodeUtilNames.HangulFirst;
                tagged.Add((0, UnicodeUtilNames.HangulLeadFirst + syllable / UnicodeUtilNames.HangulLeadStride));
                tagged.Add((0, UnicodeUtilNames.HangulVowelFirst + syllable % UnicodeUtilNames.HangulLeadStride / UnicodeUtilNames.HangulTrailCount));
                int trail = syllable % UnicodeUtilNames.HangulTrailCount;
                if (trail != 0) tagged.Add((0, UnicodeUtilNames.HangulTrailBase + trail));
            }
            else
            {
                var entry = Data.Lookup(cp);
                Term mapping = compatibility && entry.Compatibility is TupleTerm compat ? compat.Items[1] : entry.Canonical;
                if (mapping is Nil) tagged.Add((entry.Combining, cp));
                else
                {
                    foreach (TupleTerm pair in Cons.Items(mapping)) tagged.Add(((int)((Integer)pair.Items[0]).Value, (int)((Integer)pair.Items[1]).Value));
                }
            }
            if (++work % UnicodeUtilNames.WorkSlice == 0) await context.BumpReductionsAsync(UnicodeUtilNames.WorkSlice);
        }
        if (tagged.Count == 1) return Term.I(tagged[0].Cp);
        var ordered = new List<Term>(tagged.Count);
        // Stable counting sort matches lists:keysort on the byte-sized CCC field.
        var marks = new List<int>?[byte.MaxValue + 1];
        bool hasMarks = false;

        async ValueTask Flush()
        {
            if (!hasMarks) return;
            for (int i = 1; i < marks.Length; i++)
            {
                if (marks[i] is { } bucket)
                {
                    foreach (int value in bucket)
                    {
                        ordered.Add(Term.I(value));
                        if (++work % UnicodeUtilNames.WorkSlice == 0) await context.BumpReductionsAsync(UnicodeUtilNames.WorkSlice);
                    }
                    bucket.Clear();
                }
            }
            hasMarks = false;
        }
        foreach (var pair in tagged)
        {
            if (pair.Class == 0)
            {
                await Flush();
                ordered.Add(Term.I(pair.Cp));
            }
            else
            {
                (marks[pair.Class] ??= new()).Add(pair.Cp);
                hasMarks = true;
            }
            if (++work % UnicodeUtilNames.WorkSlice == 0) await context.BumpReductionsAsync(UnicodeUtilNames.WorkSlice);
        }
        await Flush();

        return Cons.From(ordered);
    }

    private static async ValueTask<Term> Compose(ProcessContext context, Term cluster)
    {
        if (cluster is Integer) return cluster;
        var values = new List<int>();
        foreach (Integer value in Cons.Items(cluster))
        {
            values.Add((int)value.Value);
            if (values.Count % UnicodeUtilNames.WorkSlice == 0) await context.BumpReductionsAsync(UnicodeUtilNames.WorkSlice);
        }
        int[] points = values.ToArray();
        if (points.Length == 0) return Nil.Value;
        int start = 0;
        while (start + 1 < points.Length && points[start] >= UnicodeUtilNames.HangulLeadFirst && points[start] <= UnicodeUtilNames.HangulLeadLast
            && (points[start + 1] < UnicodeUtilNames.HangulVowelFirst || points[start + 1] > UnicodeUtilNames.HangulVowelLast))
        {
            start++;
            if (start % UnicodeUtilNames.WorkSlice == 0) await context.BumpReductionsAsync(UnicodeUtilNames.WorkSlice);
        }
        Term composed = await Compose(context, points, start);
        if (start != 0 && composed is Integer) composed = Term.List(composed);
        while (start != 0) composed = new Cons(Term.I(points[--start]), composed);

        return composed;
    }

    private static async ValueTask<Term> Compose(ProcessContext context, int[] points, int start)
    {
        int count = points.Length - start;
        if (count == 1) return Term.I(points[start]);
        int first = points[start];
        int second = points[start + 1];
        if (first >= UnicodeUtilNames.HangulLeadFirst && first <= UnicodeUtilNames.HangulLeadLast)
        {
            if (second >= UnicodeUtilNames.HangulVowelFirst && second <= UnicodeUtilNames.HangulVowelLast)
            {
                int composed = UnicodeUtilNames.HangulFirst + (first - UnicodeUtilNames.HangulLeadFirst) * UnicodeUtilNames.HangulLeadStride
                    + (second - UnicodeUtilNames.HangulVowelFirst) * UnicodeUtilNames.HangulTrailCount;
                int next = start + 2;
                if (next < points.Length && points[next] >= UnicodeUtilNames.HangulTrailBase && points[next] <= UnicodeUtilNames.HangulTrailLast)
                    composed += points[next++] - UnicodeUtilNames.HangulTrailBase;

                return next == points.Length ? Term.I(composed) : new Cons(Term.I(composed), Cons.From(points.Skip(next).Select(cp => (Term)Term.I(cp))));
            }
            Term suffix = await Compose(context, points, start + 1);

            return new Cons(Term.I(first), suffix is Integer ? Term.List(suffix) : suffix);
        }
        if (count == 2) return Data.Compose(first, second, out int pair) ? Term.I(pair) : Term.List(Term.I(first), Term.I(second));
        int basis = first;
        int previous = Data.Lookup(first).Combining;
        var result = new List<Term>();
        var accents = new List<Term>();
        for (int index = start + 1; index < points.Length; index++)
        {
            if (index % UnicodeUtilNames.WorkSlice == 0) await context.BumpReductionsAsync(UnicodeUtilNames.WorkSlice);
            int cp = points[index];
            int combining = Data.Lookup(cp).Combining;
            if ((previous == 0 || previous < combining) && Data.Compose(basis, cp, out int combined)) basis = combined;
            else if (combining == 0)
            {
                result.Add(Term.I(basis));
                result.AddRange(accents);
                basis = cp;
                previous = 0;
                accents.Clear();
            }
            else
            {
                accents.Add(Term.I(cp));
                previous = combining;
            }
        }
        result.Add(Term.I(basis));
        result.AddRange(accents);

        return result.Count == 1 ? result[0] : Cons.From(result);
    }

    private static async ValueTask<Term> ComposeCompatibility(ProcessContext context, Term decomposed)
    {
        if (decomposed is Integer) return decomposed;
        var result = new List<Term>();
        while (await UnicodeUtilGraphemes.Gc(context, decomposed) is Cons cell)
        {
            Term composed = await Compose(context, cell.Head);
            if (composed is Integer) result.Add(composed);
            else result.AddRange(Cons.Items(composed));
            decomposed = cell.Tail;
            await context.BumpReductionsAsync(1);
        }

        return Cons.From(result);
    }
}
