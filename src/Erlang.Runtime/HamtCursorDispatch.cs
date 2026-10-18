using System.Numerics;

namespace Erlang;

internal static class HamtCursorDispatch
{
    private static readonly OtpMapOwnAtomIdentityProvider identities = new();

    internal static bool TryRead(
        MapTerm map,
        BigInteger path,
        ProcessRuntime? runtime,
        out Term cursor
    )
    {
        cursor = Nil.Value;
        if (map.Entries.Count <= MapIteratorLayout.FlatMapMaximumEntries
            || runtime is null && !map.Entries.All(entry => Supported(entry.Key)))
        {
            return false;
        }
        Term list;
        if (runtime is null)
        {
            list = HamtCursorView.ReadIntegerCursor(map, path, key => OtpMapInternalHash64.Hash(key, identities));
        }
        else
        {
            using var scope = runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);
            list = HamtCursorView.ReadIntegerCursor(map, path, scope.Hash);
        }
        var pairs = Cons.Items(list).Cast<TupleTerm>().ToArray();
        cursor = Term.A(MapIteratorAtoms.None);
        for (int index = pairs.Length - 1; index >= 0; index--)
        {
            cursor = Term.Tuple(pairs[index].Items[0], pairs[index].Items[1], cursor);
        }

        return true;
    }

    private static bool Supported(Term key)
    {
        var pending = new Stack<Term>();
        pending.Push(key);
        while (pending.TryPop(out Term? current))
        {
            switch (current)
            {
                case Integer or FloatTerm or BitString or Atom or Nil:
                    break;
                case TupleTerm tuple:
                    foreach (Term item in tuple.Items)
                        pending.Push(item);
                    break;
                case Cons list:
                    pending.Push(list.Head);
                    pending.Push(list.Tail);
                    break;
                default:
                    return false;
            }
        }

        return true;
    }
}
