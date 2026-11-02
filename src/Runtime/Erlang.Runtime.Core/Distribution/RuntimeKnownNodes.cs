using System.Runtime.CompilerServices;

namespace Erlang;

internal sealed partial class RuntimeKnownNodes(
    LocalNodeIdentity localIdentity,
    Func<Atom, RuntimeDistributionEntryIdentity?> resolveDistributionEntry,
    Action<string> observeAtom
)
{
    private readonly object gate = new();
    private readonly Dictionary<string, List<WeakReference<Term>>> nodes = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<Term, object> observed = new();
    private bool observedIdentitiesClosed;

    public void Observe(Term term)
    {
        var pending = new Stack<Term>();
        var visited = new HashSet<Term>(ReferenceEqualityComparer.Instance);

        void Enqueue(Term value)
        {
            if (visited.Add(value))
                pending.Push(value);
        }
        Enqueue(term);
        var discoveries = new List<(Term Value, string Name, bool External)>();
        var atomNames = new HashSet<string>(StringComparer.Ordinal);
        lock (gate)
        {
            if (observedIdentitiesClosed)
                return;
            while (pending.TryPop(out var value))
            {
                string? name = value switch
                {
                    Pid pid => pid.Node,
                    PortTerm port => port.Node,
                    ReferenceTerm reference => reference.Node,
                    _ => null
                };
                if (name is not null) atomNames.Add(name);
                bool local = value switch
                {
                    Pid pid => ReferenceEquals(pid.LocalIdentity, localIdentity),
                    PortTerm port => ReferenceEquals(port.LocalIdentity, localIdentity),
                    ReferenceTerm reference => ReferenceEquals(reference.LocalIdentity, localIdentity),
                    _ => false
                };
                if (name is not null && !local && !observed.TryGetValue(value, out _))
                {
                    bool external = value switch
                    {
                        Pid pid => pid.LocalIdentity is null,
                        PortTerm port => port.LocalIdentity is null,
                        ReferenceTerm reference => reference.LocalIdentity is null,
                        _ => false
                    };
                    discoveries.Add((value, name, external));
                }
                switch (value)
                {
                    case Atom atom:
                        atomNames.Add(atom.Name);
                        break;
                    case Cons list:
                        Enqueue(list.Head);
                        Enqueue(list.Tail);
                        break;
                    case NativeRecordTerm record:
                        atomNames.Add(record.Module.Name);
                        atomNames.Add(record.Name.Name);
                        foreach (Atom field in record.FieldNames) atomNames.Add(field.Name);
                        foreach (Term item in record.Values)
                            Enqueue(item);
                        break;
                    case TupleTerm tuple:
                        foreach (Term item in tuple.Items)
                            Enqueue(item);
                        break;
                    case HamtBitmapTerm429 node:
                        foreach (Term child in node.Children) Enqueue(child);
                        break;
                    case MapTerm map:
                        foreach (var pair in map.Entries)
                        {
                            Enqueue(pair.Key);
                            Enqueue(pair.Value);
                        }
                        break;
                    case FunctionTerm function:
                        if (function.ExternalIdentity is { } external)
                        {
                            atomNames.Add(external.Module.Name);
                            atomNames.Add(external.Function.Name);
                        }
                        if (function.StoredLocalIdentity is { } localFunction)
                        {
                            atomNames.Add(localFunction.Module.Name);
                            if (localFunction.Name is { } functionName) atomNames.Add(functionName.Name);
                        }
                        foreach (Term item in function.Environment)
                            Enqueue(item);
                        break;
                }
            }
        }
        foreach (string atomName in atomNames) observeAtom(atomName);
        foreach (var discovery in discoveries)
        {
            // Never acquire the runtime gate/call a resolver while holding this gate.
            // Foreign runtime local identities retain existing observation behavior only.
            object? retention = discovery.External
                ? resolveDistributionEntry(Term.A(discovery.Name))
                : new object();
            lock (gate)
            {
                if (observedIdentitiesClosed)
                    return;
                ObjectDisposedException.ThrowIf(retention is null, this);
                if (observed.TryGetValue(discovery.Value, out _))
                    continue;
                observed.Add(discovery.Value, retention!);
                if (!nodes.TryGetValue(discovery.Name, out var references))
                    nodes.Add(discovery.Name, references = new());
                references.Add(new(discovery.Value));
            }
        }
    }

    internal bool HasRetainedExternalEpoch(string name, uint creation)
    {
        lock (gate)
        {
            if (HasPhysicalNodeEpoch(name, creation) || reservedEpochs.ContainsKey((name, creation)))
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

            return nodes.Keys.Concat(reservedEpochs.Keys.Select(epoch => epoch.Node)).Distinct(StringComparer.Ordinal)
                .Where(name => name != localIdentity.Snapshot.Node).Order(StringComparer.Ordinal).ToArray();
        }
    }
}
