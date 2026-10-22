namespace Erlang;

internal static class EtsCursorPattern420
{
    internal static bool Hole(Term term) => term is Atom { Name: MatchSpecNames.Wildcard } || MatchSpecCompiler.Variable(term, out _);

    internal static bool ContainsMap(Term term)
    {
        var pending = new Stack<Term>();
        pending.Push(term);
        while (pending.TryPop(out var current))
        {
            if (current is MapTerm) return true;
            if (current is TupleTerm tuple) foreach (var item in tuple.Items) pending.Push(item);
            else if (current is Cons cell)
            {
                pending.Push(cell.Head);
                pending.Push(cell.Tail);
            }
        }

        return false;
    }

    internal static bool FullyBound(Term term)
    {
        var pending = new Stack<Term>();
        pending.Push(term);
        while (pending.TryPop(out var current))
        {
            if (Hole(current) || current is MapTerm) return false;
            if (current is TupleTerm tuple) foreach (var item in tuple.Items) pending.Push(item);
            else if (current is Cons cell)
            {
                pending.Push(cell.Head);
                pending.Push(cell.Tail);
            }
        }

        return true;
    }

    internal static EtsCursorBounds420 Analyze(Term specification, long position)
    {
        EtsCursorBoundness420 overall = EtsCursorBoundness420.Impossible;
        Term? least = null, most = null;
        var keys = new List<Term>();
        bool hashBound = true;
        foreach (var term in Cons.Items(specification))
        {
            var head = ((TupleTerm)term).Items[0];
            Term? key = head is TupleTerm tuple && position <= tuple.Items.Count ? tuple.Items[(int)position - 1] : null;
            EtsCursorBoundness420 kind = Hole(head) ? EtsCursorBoundness420.Unbound : key is null ? EtsCursorBoundness420.Impossible : FullyBound(key) ? EtsCursorBoundness420.Bound : !Hole(key) && !ContainsMap(key) ? EtsCursorBoundness420.Partial : EtsCursorBoundness420.Unbound;
            if ((int)kind < (int)overall) overall = kind;
            if (kind is EtsCursorBoundness420.Partial or EtsCursorBoundness420.Bound)
            {
                if (least is null || Extreme(key!, least, false)) least = key;
                if (most is null || Extreme(key!, most, true)) most = key;
            }
            if (Hole(head) || key is not null && !FullyBound(key)) hashBound = false;
            else if (key is not null && !keys.Any(old => old.Equals(key))) keys.Add(key);
        }
        if (overall == EtsCursorBoundness420.Bound && TermOrder.Compare(least!, most!) != 0) overall = EtsCursorBoundness420.Partial;

        return new(
            overall,
            least,
            most,
            hashBound ? keys : null
        );
    }

    internal static int PartlyCompare(Term pattern, Term actual)
    {
        var pending = new Stack<(Term Pattern, Term Actual)>();
        pending.Push((pattern, actual));
        while (pending.TryPop(out var pair))
        {
            if (Hole(pair.Pattern)) return 0;
            if (ReferenceEquals(pair.Pattern, pair.Actual)) continue;
            if (pair.Pattern is TupleTerm left && pair.Actual is TupleTerm right)
            {
                int size = left.Items.Count.CompareTo(right.Items.Count);
                if (size != 0) return size;
                for (int i = left.Items.Count - 1; i >= 0; i--) pending.Push((left.Items[i], right.Items[i]));
            }
            else if (pair.Pattern is Cons a && pair.Actual is Cons b)
            {
                pending.Push((a.Tail, b.Tail));
                pending.Push((a.Head, b.Head));
            }
            else
            {
                int compared = TermOrder.Compare(pair.Pattern, pair.Actual);
                if (compared != 0) return compared;
            }
        }

        return 0;
    }

    private static bool Extreme(Term a, Term b, bool greater)
    {
        bool done = false;

        return Extreme(
            a,
            b,
            greater,
            ref done
        );
    }

    private static bool Extreme(
        Term a,
        Term b,
        bool greater,
        ref bool done
    )
    {
        if (Hole(a))
        {
            done = true;

            return !Hole(b);
        }
        if (Hole(b))
        {
            done = true;

            return false;
        }
        if (ReferenceEquals(a, b)) return false;
        if (a.GetType() != b.GetType())
        {
            done = true;
            int compare = TermOrder.Compare(a, b);

            return greater ? compare > 0 : compare < 0;
        }
        if (a is TupleTerm left && b is TupleTerm right)
        {
            int size = left.Items.Count.CompareTo(right.Items.Count);
            if (size != 0) return greater ? size > 0 : size < 0;
            for (int i = 0; i < left.Items.Count; i++)
            {
                bool result = Extreme(
                    left.Items[i],
                    right.Items[i],
                    greater,
                    ref done
                );
                if (result || done) return result;
            }

            return false;
        }
        if (a is Cons first && b is Cons second)
        {
            bool result = Extreme(
                first.Head,
                second.Head,
                greater,
                ref done
            );

            return result || done ? result : Extreme(
                first.Tail,
                second.Tail,
                greater,
                ref done
            );
        }
        int order = TermOrder.Compare(a, b);
        if (order != 0) done = true;

        return greater ? order > 0 : order < 0;
    }
}
