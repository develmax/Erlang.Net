using System.Numerics;

namespace Erlang.Otp;

internal static class ErrorLoggerTermLimiter
{
    private static readonly Atom Ellipsis = Term.A(ErrorLoggerTermLimitNames.Ellipsis);
    private static readonly Atom Unlimited = Term.A(ErrorLoggerTermLimitNames.Unlimited);

    public static Term NormalizeDepth(Term configured)
    {
        if (configured is Integer integer)
            return new Integer(BigInteger.Max(integer.Value, ErrorLoggerTermLimitNames.MinimumFormatDepth));
        if (configured.Equals(Unlimited))
            return configured;

        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), configured));
    }

    public static Term Limit(Term term, Term depth)
    {
        if (depth.Equals(Unlimited))
            return term;
        if (depth is not Integer integer || integer.Value < ErrorLoggerTermLimitNames.UnlimitedDepth)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        if (integer.Value == ErrorLoggerTermLimitNames.UnlimitedDepth)
            return term;
        if (ErrorLoggerTermLimitProbe.Fits(term, integer.Value, PrintableList))
            return term;
        var work = new Stack<Action>();
        var results = new Stack<Term>();

        void Construct(IReadOnlyList<(Term Value, BigInteger Depth)> children, Func<Term[], Term> factory)
        {
            work.Push(() =>
            {
                var values = new Term[children.Count];
                for (int index = values.Length - 1; index >= 0; index--)
                    values[index] = results.Pop();
                results.Push(factory(values));
            });
            for (int index = children.Count - 1; index >= 0; index--)
                Schedule(children[index].Value, children[index].Depth);
        }

        void ScheduleCons(Cons list, BigInteger remaining)
        {
            work.Push(() =>
            {
                Term tail = results.Pop();
                Term head = results.Pop();
                results.Push(new Cons(head, tail));
            });
            ScheduleTail(list.Tail, remaining - 1);
            Schedule(list.Head, remaining - 1);
        }

        void ScheduleTail(Term value, BigInteger remaining)
        {
            work.Push(() =>
            {
                if (value is Nil)
                    results.Push(value);
                else if (remaining.IsOne)
                    results.Push(Term.List(Ellipsis));
                else if (value is Cons list)
                    ScheduleCons(list, remaining);
                else
                    Schedule(value, remaining - 1);
            });
        }

        void Schedule(Term value, BigInteger remaining)
        {
            work.Push(() =>
            {
                if (remaining.IsZero)
                {
                    results.Push(Ellipsis);

                    return;
                }
                if (value is Cons list)
                {
                    if (remaining.IsOne)
                        results.Push(Term.List(Ellipsis));
                    else if (PrintableList(list))
                        results.Push(list);
                    else
                        ScheduleCons(list, remaining);

                    return;
                }
                if (value is TupleTerm tuple && tuple.Items.Count > 0)
                {
                    if (remaining.IsOne)
                    {
                        results.Push(Term.Tuple(Ellipsis));

                        return;
                    }
                    var children = new List<(Term, BigInteger)> { (tuple.Items[0], remaining - 1) };
                    BigInteger tailDepth = remaining - 1;
                    for (int index = 1; index < tuple.Items.Count; index++)
                    {
                        if (tailDepth.IsOne)
                        {
                            children.Add((Ellipsis, ErrorLoggerTermLimitNames.UnlimitedDepth));
                            break;
                        }
                        children.Add((tuple.Items[index], tailDepth - 1));
                        tailDepth--;
                    }
                    Construct(children, values => new TupleTerm(values));

                    return;
                }
                if (value is MapTerm map)
                {
                    int count = (int)BigInteger.Min(map.Entries.Count, remaining + 1);
                    var entries = IteratorPrefix(map, count);
                    Construct(
                        entries.Select(entry => (entry.Value, remaining - 1)).ToArray(),
                        values => new MapTerm(
                        entries.Select((entry, index) => new KeyValuePair<Term, Term>(entry.Key, values[index]))
                    )
                    );

                    return;
                }
                results.Push(value);
            });
        }

        Schedule(term, integer.Value);
        while (work.TryPop(out Action? action))
            action();

        return results.Pop();
    }

    private static KeyValuePair<Term, Term>[] IteratorPrefix(MapTerm map, int count)
    {
        var entries = new List<KeyValuePair<Term, Term>>();
        Term iterator = MapIteration.Iterator(map);
        while (entries.Count < count && MapIteration.Next(iterator) is TupleTerm { Items: [var key, var value, var next] })
        {
            entries.Add(new(key, value));
            iterator = next;
        }

        return entries.ToArray();
    }

    private static bool PrintableList(Term value)
    {
        while (value is Cons list)
        {
            if (list.Head is not Integer integer || integer.Value < 0 || integer.Value > ErrorLoggerTermLimitNames.PrintableLatin1Last)
                return false;
            int character = (int)integer.Value;
            if (character is not (>= ErrorLoggerTermLimitNames.PrintableAsciiFirst and <= ErrorLoggerTermLimitNames.PrintableAsciiLast or
                >= ErrorLoggerTermLimitNames.PrintableLatin1First and <= ErrorLoggerTermLimitNames.PrintableLatin1Last or
                '\n' or '\r' or '\t' or '\v' or '\b' or '\f' or '\u001b'))
                return false;
            value = list.Tail;
        }

        return value is Nil;
    }
}
