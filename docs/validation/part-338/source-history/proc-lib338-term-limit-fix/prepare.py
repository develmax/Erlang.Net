from pathlib import Path
import hashlib,json
base=Path('artifacts/proc-lib338-candidate');out=Path('artifacts/proc-lib338-term-limit-fix');src=out/'source';rel=Path('src/Erlang.Otp.Behaviours/ErrorLoggerTermLimiter.cs');b=(base/rel).read_bytes();(out/'before-ErrorLoggerTermLimiter.cs').write_bytes(b);t=b.decode('utf-8-sig');marker='        var work = new Stack<Action>();';assert t.count(marker)==1;t=t.replace(marker,'        if (ErrorLoggerTermLimitProbe.Fits(term, integer.Value, PrintableList))\n            return term;\n'+marker)
old='                    var entries = map.Entries.Take(count).ToArray();';assert t.count(old)==1;t=t.replace(old,'                    var entries = IteratorPrefix(map, count);')
marker='    private static bool PrintableList(Term value)';assert t.count(marker)==1;t=t.replace(marker,'''    private static KeyValuePair<Term, Term>[] IteratorPrefix(MapTerm map, int count)
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

'''+marker);(src/rel).write_text(t,encoding='utf-8')
p=src/'src/Erlang.Otp.Behaviours/ErrorLoggerTermLimitProbe.cs';p.write_text('''using System.Numerics;

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
''',encoding='utf-8')
(out/'parent.json').write_text(json.dumps(dict(Path=rel.as_posix(),Bytes=len(b),Sha256=hashlib.sha256(b).hexdigest().upper(),Parent='actualprivate338currentlySDKowned; no mutation'),indent=2)+'\n');print('production source proposal ready')
