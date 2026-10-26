namespace Erlang;

internal static class RuntimeNodeQueryRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            RuntimeNodeQueryNames.IsAlive,
            0,
            (context, _) => ValueTask.FromResult<Term>(CoreModules.Boolean(
                context.Runtime.Node != ProcessNodeDefaults.Local || !context.Runtime.PersistentTerms.Get(
                    context,
                    RuntimeProcessLivenessNames499.DynamicNodeNameKey,
                    CoreModules.Boolean(false)
                ).Equals(CoreModules.Boolean(false))
            ))
        );
        foreach (int arity in new[] { 0, 1, 2 })
            registry.Register(
                ErlangModuleNames.Module,
                RuntimeNodeQueryNames.Nodes,
                arity,
                Query
            );
    }

    private static ValueTask<Term> Query(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        RuntimeNodeQuery query;
        try
        {
            query = RuntimeNodeQuery.Parse(arguments.Count == 0 ? Term.A(RuntimeNodeQueryNames.Visible) : arguments[0], arguments.Count == 2 ? arguments[1] : null);
        }
        catch (ErlangException)
        {
            var info = new MapTerm([new(Term.A(RuntimeNodeQueryNames.ErrorModule), Term.A(RuntimeNodeQueryNames.ErrorFormatter))]);
            throw new ErlangException(ErlangErrorReasons.BadArgument)
            {
                ExplicitStackTrace = true,
                StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), info, RuntimeNodeQueryNames.Nodes)
            };
        }
        Term result = Nil.Value;
        var names = new List<(Atom Name, Atom Type, uint? ConnectionId)>();
        var snapshot = context.Runtime.DistributionPending.QueryNodeSnapshot();
        if (query.This)
            names.Add((context.Runtime.NodeAtom, Term.A(RuntimeNodeQueryNames.This), null));
        foreach (bool visible in new[] { true, false })
        {
            if (visible ? !query.Visible : !query.Hidden)
                continue;
            foreach (var node in snapshot.Where(node => node.Visible == visible))
                names.Add((node.Node, Term.A(visible ? RuntimeNodeQueryNames.Visible : RuntimeNodeQueryNames.Hidden), node.ConnectionId));
        }
        if (query.Known)
        {
            var connected = snapshot.Where(node => node.ConnectionId is not null).Select(node => node.Node.Name).ToHashSet(StringComparer.Ordinal);
            foreach (string name in context.Runtime.KnownNodes.Snapshot().Concat(snapshot.Where(node => node.ConnectionId is null).Select(node => node.Node.Name)).Distinct(StringComparer.Ordinal))
                if (name != context.Runtime.Node && !connected.Contains(name))
                    names.Add((Term.A(name), Term.A(RuntimeNodeQueryNames.Known), null));
        }
        foreach (var node in names)
        {
            Term item = node.Name;
            if (query.Info)
            {
                var entries = new List<KeyValuePair<Term, Term>>();
                if (query.ConnectionId)
                    entries.Add(new(
                        Term.A(RuntimeNodeQueryNames.ConnectionId),
                        node.ConnectionId is uint id ? Term.I(id) : Term.A(RuntimeNodeQueryNames.Undefined)
                    ));
                if (query.NodeType)
                    entries.Add(new(Term.A(RuntimeNodeQueryNames.NodeType), node.Type));
                item = Term.Tuple(node.Name, new MapTerm(entries));
            }
            result = new Cons(item, result);
        }

        return ValueTask.FromResult(result);
    }
}
