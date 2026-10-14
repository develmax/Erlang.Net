using System.Runtime.CompilerServices;

namespace Erlang;

internal sealed partial class RuntimeKnownNodes(LocalNodeIdentity localIdentity)
{
    private readonly object gate = new();
    private readonly Dictionary<string, List<WeakReference<Term>>> nodes = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<Term, object> observed = new();

    public void Observe(Term term)
    {
        var pending = new Stack<Term>();
        var visited = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        pending.Push(term);
        lock (gate)
            while (pending.TryPop(out var value))
            {
                if (!visited.Add(value))
                    continue;
                string? name = value switch
                {
                    Pid pid => pid.Node,
                    PortTerm port => port.Node,
                    ReferenceTerm reference => reference.Node,
                    _ => null
                };
                bool local = value switch
                {
                    Pid pid => ReferenceEquals(pid.LocalIdentity, localIdentity),
                    PortTerm port => ReferenceEquals(port.LocalIdentity, localIdentity),
                    ReferenceTerm reference => ReferenceEquals(reference.LocalIdentity, localIdentity),
                    _ => false
                };
                if (name is not null && !local && !observed.TryGetValue(value, out _))
                {
                    observed.Add(value, new object());
                    if (!nodes.TryGetValue(name, out var references))
                        nodes.Add(name, references = new());
                    references.Add(new(value));
                }
                switch (value)
                {
                    case Cons list:
                        pending.Push(list.Head);
                        pending.Push(list.Tail);
                        break;
                    case NativeRecordTerm record:
                        foreach (Term item in record.Values)
                            pending.Push(item);
                        break;
                    case TupleTerm tuple:
                        foreach (Term item in tuple.Items)
                            pending.Push(item);
                        break;
                    case MapTerm map:
                        foreach (var pair in map.Entries)
                        {
                            pending.Push(pair.Key);
                            pending.Push(pair.Value);
                        }
                        break;
                    case FunctionTerm function:
                        foreach (Term item in function.Environment)
                            pending.Push(item);
                        break;
                }
            }
    }

    internal bool HasRetainedExternalEpoch(string name, uint creation)
    {
        lock (gate)
        {
            if (reservedEpochs.ContainsKey((name, creation)))
                return true;
            if (!nodes.TryGetValue(name, out var references))
                return false;

            return references.Any(reference => reference.TryGetTarget(out var term) && (term switch
            {
                Pid pid => pid.LocalIdentity is null && pid.Creation == creation,
                PortTerm port => port.LocalIdentity is null && port.Creation == creation,
                ReferenceTerm value => value.LocalIdentity is null && value.Creation == creation,
                _ => false
            }));
        }
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (gate)
        {
            foreach (var pair in nodes.ToArray())
            {
                pair.Value.RemoveAll(reference => !reference.TryGetTarget(out _));
                if (pair.Value.Count == 0)
                    nodes.Remove(pair.Key);
            }

            return nodes.Keys.Where(name => name != localIdentity.Snapshot.Node).Order(StringComparer.Ordinal).ToArray();
        }
    }
}
