namespace Erlang.Compiler;

internal sealed class SourceClosedConstant820
{
    private readonly Dictionary<Expr, Term> values = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Expr> rejected = new(ReferenceEqualityComparer.Instance);

    internal bool TryRead(Expr expression, out Term value)
    {
        var pending = new Stack<(Expr Expression, bool Expanded)>();
        pending.Push((expression, false));
        while (pending.TryPop(out var work))
        {
            Expr candidate = work.Expression;
            if (values.ContainsKey(candidate) || rejected.Contains(candidate)) continue;
            if (!work.Expanded)
            {
                switch (candidate)
                {
                    case Expr.Literal literal:
                        values.Add(candidate, literal.Value);
                        break;
                    case Expr.Tuple tuple:
                        pending.Push((candidate, true));
                        PushItems(tuple.Items, pending);
                        break;
                    case Expr.List list:
                        pending.Push((candidate, true));
                        if (list.Tail is not null) pending.Push((list.Tail, false));
                        PushItems(list.Items, pending);
                        break;
                    case Expr.Bits bits:
                        if (SourceConstantBits820.TryRead(bits, out Term binary)) values.Add(candidate, binary);
                        else rejected.Add(candidate);
                        break;
                    default:
                        rejected.Add(candidate);
                        break;
                }
                continue;
            }
            switch (candidate)
            {
                case Expr.Tuple tuple when TryItems(tuple.Items, out Term[] items):
                    values.Add(candidate, new TupleTerm(items));
                    break;
                case Expr.List list:
                    if (!TryItems(list.Items, out Term[] heads))
                    {
                        rejected.Add(candidate);
                        break;
                    }
                    Term? tail = null;
                    if (list.Tail is not null && !values.TryGetValue(list.Tail, out tail))
                    {
                        rejected.Add(candidate);
                        break;
                    }
                    values.Add(candidate, Cons.From(heads, tail));
                    break;
                default:
                    rejected.Add(candidate);
                    break;
            }
        }

        return values.TryGetValue(expression, out value!);
    }

    private static void PushItems(IReadOnlyList<Expr> items, Stack<(Expr Expression, bool Expanded)> pending)
    {
        for (int index = items.Count - 1; index >= 0; index--) pending.Push((items[index], false));
    }

    private bool TryItems(IReadOnlyList<Expr> expressions, out Term[] result)
    {
        result = new Term[expressions.Count];
        for (int index = 0; index < expressions.Count; index++)
            if (!values.TryGetValue(expressions[index], out result[index]!)) return false;

        return true;
    }
}
