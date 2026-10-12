using System.Runtime.CompilerServices;

namespace Erlang;

internal sealed class RuntimeKnownNodes(string localNode)
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
                if (name is not null && name != localNode && !observed.TryGetValue(value, out _))
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

            return nodes.Keys.Order(StringComparer.Ordinal).ToArray();
        }
    }
}
