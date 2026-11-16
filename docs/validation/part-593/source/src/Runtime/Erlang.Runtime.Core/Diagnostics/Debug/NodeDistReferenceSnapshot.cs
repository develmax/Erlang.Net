namespace Erlang;

// A census of actual retained roots. Equality groups referrers; identity
// deduplication applies only to physical headers, never equal identifiers.
internal sealed class NodeDistReferenceSnapshot
{
    private readonly Dictionary<LocalNodeSnapshot, Dictionary<Term, Dictionary<string, long>>> nodes = new();
    private readonly Dictionary<string, Dictionary<Term, Dictionary<string, long>>> distributions = new(StringComparer.Ordinal);
    private readonly HashSet<object> headers = new(ReferenceEqualityComparer.Instance);

    private readonly HashSet<object> matchPrograms = new(ReferenceEqualityComparer.Instance);

    internal static Term Owner(string kind, Term identity) => Term.Tuple(Term.A(kind), identity);

    internal void Node(
        LocalNodeSnapshot epoch,
        Term owner,
        string kind,
        object? header = null
    )
    {
        if (header is not null && !headers.Add(header)) return;
        if (!nodes.TryGetValue(epoch, out var references)) nodes.Add(epoch, references = new());
        Increment(references, owner, kind);
    }

    internal void Distribution(
        string name,
        Term owner,
        string kind,
        object? header = null
    )
    {
        if (header is not null && !headers.Add(header)) return;
        if (!distributions.TryGetValue(name, out var references)) distributions.Add(name, references = new());
        Increment(references, owner, kind);
    }

    private static void Increment(Dictionary<Term, Dictionary<string, long>> references, Term owner, string kind)
    {
        if (!references.TryGetValue(owner, out var counts)) references.Add(owner, counts = new(StringComparer.Ordinal));
        counts[kind] = checked(counts.GetValueOrDefault(kind) + 1);
    }

    internal void Graph(Term root, Term owner, string kind)
    {
        var visited = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(root);
        while (pending.TryPop(out Term? value))
        {
            if (!visited.Add(value)) continue;
            switch (value)
            {
                case Pid { LocalIdentity: null } pid:
                    Node(
                        pid.WireIdentity,
                        owner,
                        kind,
                        pid.OwnedExternalStorage
                    );
                    break;
                case PortTerm { LocalIdentity: null } port:
                    Node(
                        port.WireIdentity,
                        owner,
                        kind,
                        port.OwnedExternalStorage
                    );
                    break;
                case ReferenceTerm { LocalIdentity: null } reference:
                    Node(
                        reference.WireIdentity,
                        owner,
                        kind,
                        reference.OwnedExternalStorage
                    );
                    break;
                case ResourceReferenceTerm resource when resource.Entry.Kind is ReferenceResourceKind.DistributionEntry or ReferenceResourceKind.ConnectedDistributionHandle:
                    Distribution(
                        resource.Entry.Read<RuntimeDistributionPendingEntry>().Node.Name,
                        owner,
                        kind,
                        resource.OwnedStorage562
                    );
                    break;
                case ResourceReferenceTerm resource when resource.Entry.Kind == ReferenceResourceKind.CompiledMatchSpecification419:
                    if (resource.Entry.Read<EtsCompiledProgram419>().Executable.NodeDistReferenceProgram is { } program)
                        MatchProgram(program, Owner(NodeDistReferenceNames.MatchSpec, new Integer(resource.FullId)));
                    break;
            }
            foreach (Term child in DebugSharingGraph425.Children(value)) pending.Push(child);
        }
    }

    internal void MatchProgram(MatchSpecProgram program, Term owner)
    {
        if (!matchPrograms.Add(program)) return;
        Graph(Cons.From(program.NodeDistReferenceClauses), owner, NodeDistReferenceNames.Binary);
    }

    internal Term Complete(LocalNodeSnapshot local)
    {
        Term system = Owner(NodeDistReferenceNames.System, Term.A(ProcessRegistryAtoms.Undefined));
        Node(local, system, NodeDistReferenceNames.System);
        Distribution(local.Node, system, NodeDistReferenceNames.System);
        foreach (LocalNodeSnapshot epoch in nodes.Keys)
            Distribution(
                epoch.Node,
                Owner(NodeDistReferenceNames.Node, Term.Tuple(Term.A(epoch.Node), Term.I(epoch.Creation))),
                NodeDistReferenceNames.Node
            );

        return Term.Tuple(
            Term.Tuple(
                Term.A(NodeDistReferenceNames.Nodes),
                Cons.From(nodes.Select(pair => Row(
                Term.Tuple(Term.A(pair.Key.Node), Term.I(pair.Key.Creation)), pair.Value, NodeDistReferenceNames.NodeKinds)))
            ),
            Term.Tuple(
                Term.A(NodeDistReferenceNames.Distributions),
                Cons.From(distributions.Select(pair => Row(
                Term.A(pair.Key), pair.Value, NodeDistReferenceNames.DistKinds)))
            )
        );
    }

    private static Term Row(Term identity, Dictionary<Term, Dictionary<string, long>> references, string[] order)
        => Term.Tuple(
            identity,
            new Integer(references.Values.Sum(counts => counts.Values.Sum())),
            Cons.From(references.Select(pair =>
            Term.Tuple(
                pair.Key,
                Cons.From(order.Where(pair.Value.ContainsKey).Select(kind =>
                Term.Tuple(Term.A(kind), new Integer(pair.Value[kind]))))
            )))
        );
}
