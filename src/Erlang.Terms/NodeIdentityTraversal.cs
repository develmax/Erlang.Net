namespace Erlang;

internal static class NodeIdentityTraversal
{
    internal static LocalNodeIdentity[] KeyNodes(Dictionary<Term, Term> entries)
    {
        Stack<Term>? pending = null;
        foreach (var entry in entries)
        {
            Term key = entry.Key;
            if (MayContainLocalIdentity(key))
                (pending ??= new()).Push(key);
        }
        if (pending is null)
            return [];
        var identities = new HashSet<LocalNodeIdentity>();
        var visited = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        while (pending.TryPop(out var term))
        {
            if (!visited.Add(term))
                continue;
            LocalNodeIdentity? identity = term switch
            {
                Pid pid => pid.LocalIdentity,
                PortTerm port => port.LocalIdentity,
                ReferenceTerm reference => reference.LocalIdentity,
                _ => null
            };
            if (identity is not null)
                identities.Add(identity);
            switch (term)
            {
                case Cons list:
                    pending.Push(list.Head);
                    pending.Push(list.Tail);
                    break;
                case TupleTerm tuple:
                    foreach (var item in tuple.Items)
                        pending.Push(item);
                    break;
                case NativeRecordTerm record:
                    foreach (var item in record.Values)
                        pending.Push(item);
                    break;
                case FunctionTerm function:
                    foreach (var item in function.Environment)
                        pending.Push(item);
                    break;
                case MapTerm map:
                    foreach (var entry in map.Entries)
                    {
                        pending.Push(entry.Key);
                        pending.Push(entry.Value);
                    }
                    break;
            }
        }

        return identities.ToArray();
    }

    private static bool MayContainLocalIdentity(Term term) => term switch
    {
        Pid pid => pid.LocalIdentity is not null,
        PortTerm port => port.LocalIdentity is not null,
        ReferenceTerm reference => reference.LocalIdentity is not null,
        Cons or TupleTerm or NativeRecordTerm or MapTerm => true,
        FunctionTerm function => function.Environment.Count != 0,
        _ => false
    };
}
