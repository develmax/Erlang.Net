namespace Erlang;

internal static class RuntimeMapHashTraversal
{
    internal static IEnumerable<Term> Children(Term term)
    {
        switch (term)
        {
            case Cons list:
                yield return list.Head;
                yield return list.Tail;
                break;
            case TupleTerm tuple:
                foreach (Term item in tuple.Items)
                    yield return item;
                break;
            case HamtBitmapTerm429 node:
                foreach (Term child in node.Children) yield return child;
                break;
            case MapTerm map:
                foreach (var pair in map.Entries)
                {
                    yield return pair.Key;
                    yield return pair.Value;
                }
                break;
            case NativeRecordTerm record:
                foreach (Term item in record.Values)
                    yield return item;
                break;
            case FunctionTerm function:
                foreach (Term item in function.Environment)
                    yield return item;
                break;
        }
    }
}
