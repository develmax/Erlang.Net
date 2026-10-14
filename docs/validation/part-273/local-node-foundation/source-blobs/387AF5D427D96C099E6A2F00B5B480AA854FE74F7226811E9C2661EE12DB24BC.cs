namespace Erlang;

internal static class RuntimeNodeQueryRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            RuntimeNodeQueryNames.IsAlive,
            0,
            (context, _) => ValueTask.FromResult<Term>(Term.A(context.Runtime.IsDistributionAlive ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False))
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
        var names = new List<(Atom Name, Atom Type)>();
        if (query.This)
            names.Add((context.Runtime.NodeAtom, Term.A(RuntimeNodeQueryNames.This)));
        if (query.Known)
            foreach (string name in context.Runtime.KnownNodes.Snapshot())
                names.Add((Term.A(name), Term.A(RuntimeNodeQueryNames.Known)));
        foreach (var node in names)
        {
            Term item = node.Name;
            if (query.Info)
            {
                var entries = new List<KeyValuePair<Term, Term>>();
                if (query.ConnectionId)
                    entries.Add(new(Term.A(RuntimeNodeQueryNames.ConnectionId), Term.A(RuntimeNodeQueryNames.Undefined)));
                if (query.NodeType)
                    entries.Add(new(Term.A(RuntimeNodeQueryNames.NodeType), node.Type));
                item = Term.Tuple(node.Name, new MapTerm(entries));
            }
            result = new Cons(item, result);
        }

        return ValueTask.FromResult(result);
    }
}
