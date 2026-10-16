using System.Numerics;

namespace Erlang.Otp;

internal static class ErrorLoggerTermLimitProbe
{
    public static bool Fits(Term term, BigInteger depth, Func<Term, bool> printableList)
    {
        bool fits = true;
        var work = new Stack<Action>();

        void ScheduleTail(Term value, BigInteger remaining)
        {
            work.Push(() =>
            {
                if (value is Nil)
                    return;
                if (remaining.IsOne)
                {
                    fits = false;

                    return;
                }
                if (value is Cons list)
                {
                    ScheduleTail(list.Tail, remaining - 1);
                    Schedule(list.Head, remaining - 1);
                }
                else
                    Schedule(value, remaining - 1);
            });
        }

        void ScheduleTuple(TupleTerm tuple, int index, BigInteger remaining)
        {
            work.Push(() =>
            {
                if (index >= tuple.Items.Count)
                    return;
                if (remaining.IsOne)
                {
                    fits = false;

                    return;
                }
                ScheduleTuple(tuple, index + 1, remaining - 1);
                Schedule(tuple.Items[index], remaining - 1);
            });
        }

        void ScheduleMap(Term iterator, BigInteger remaining)
        {
            work.Push(() =>
            {
                if (remaining.IsZero)
                {
                    fits = false;

                    return;
                }
                if (MapIteration.Next(iterator) is TupleTerm { Items: [var key, var value, var next] })
                {
                    ScheduleMap(next, remaining - 1);
                    Schedule(value, remaining - 1);
                    Schedule(key, remaining - 1);
                }
            });
        }

        void Schedule(Term value, BigInteger remaining)
        {
            work.Push(() =>
            {
                if (remaining.IsZero)
                {
                    fits = false;

                    return;
                }
                if (value is Cons list)
                {
                    if (remaining.IsOne)
                        fits = false;
                    else if (!printableList(value))
                    {
                        ScheduleTail(list.Tail, remaining - 1);
                        Schedule(list.Head, remaining - 1);
                    }
                }
                else if (value is MapTerm map)
                    ScheduleMap(MapIteration.Iterator(map), remaining);
                else if (value is TupleTerm tuple)
                    ScheduleTuple(tuple, 0, remaining);
            });
        }

        Schedule(term, depth);
        while (fits && work.TryPop(out Action? action))
            action();

        return fits;
    }
}
