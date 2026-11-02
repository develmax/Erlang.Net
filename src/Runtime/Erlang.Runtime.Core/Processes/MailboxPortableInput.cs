namespace Erlang;

// This is a temporary DTO optimization, never native ownership or execution
// permission. All non-whitelisted terms take the unchanged retention/copy path.
internal static class MailboxPortableInput
{
    internal static bool CanReuse(Term source)
    {
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(source);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            if (ProcessTermHeap506.Storage(value) is not null) return false;
            switch (value)
            {
                case Nil or Atom or Integer or FloatTerm:
                    break;
                case Cons list:
                    pending.Push(list.Head);
                    pending.Push(list.Tail);
                    break;
                case TupleTerm tuple:
                    foreach (Term child in tuple.Items) pending.Push(child);
                    break;
                case MapTerm map when map.Entries.Count == 0 && map.NativeEquality505 is null:
                    break;
                case BitString binary when binary.IsPlainPortableMailboxInput:
                    break;
                default:
                    // Functions/records/native resources/identifiers/foreign or
                    // unknown values never acquire reuse authority by equality.
                    return false;
            }
        }

        return true;
    }
}
