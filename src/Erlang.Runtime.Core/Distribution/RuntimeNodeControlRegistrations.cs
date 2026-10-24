namespace Erlang;

internal static class RuntimeNodeControlRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 2, 3 })
            registry.Register(
                ErlangModuleNames.Module,
                RuntimeNodeControlNames.Monitor,
                arity,
                Monitor
            );
        registry.Register(
            ErlangModuleNames.Module,
            RuntimeNodeControlNames.MonitorTrap,
            3,
            MonitorTrap
        );
        registry.Register(
            ErlangModuleNames.Module,
            RuntimeNodeControlNames.Disconnect,
            1,
            (context, arguments) => RuntimeNodeKernelRequest.Call(context, Term.Tuple(Term.A(RuntimeNodeControlNames.DisconnectRequest), arguments[0]))
        );
    }

    private static async ValueTask<Term> Monitor(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term options = arguments.Count == 3 ? arguments[2] : Nil.Value;
        bool passive = false;
        try
        {
            foreach (Term option in Cons.Items(options))
            {
                if (option is not Atom { Name: RuntimeNodeControlNames.AllowPassive })
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                passive = true;
            }
        }
        catch (ErlangException)
        {
            throw Invalid(
                context,
                arguments,
                ErlangErrorReasons.BadArgument,
                true
            );
        }
        if (arguments[0] is not Atom node)
            throw Invalid(
                context,
                arguments,
                ErlangErrorReasons.BadArgument,
                false
            );
        if (node.Name != ProcessNodeDefaults.Local && !context.Runtime.IsDistributionAlive)
            throw Invalid(
                context,
                arguments,
                RuntimeNodeControlNames.NotAlive,
                false
            );
        if (arguments[1] is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } flag)
            throw Invalid(
                context,
                arguments,
                ErlangErrorReasons.BadArgument,
                false
            );
        if (node.Equals(context.Runtime.NodeAtom))
            return Term.A(ErlangBooleanAtoms.True);
        if (context.Runtime.DistributionPending.TryMonitorNode458(
            context,
            node,
            flag.Name == ErlangBooleanAtoms.True,
            passive
        ))
            return Term.A(ErlangBooleanAtoms.True);
        if (!passive)
            return Down(context, node);
        while (true)
        {
            Term result = await RuntimeNodeKernelRequest.Call(context, Term.Tuple(Term.A(RuntimeNodeControlNames.PassiveRequest), node));
            if (result is Atom { Name: RuntimeNodeControlNames.Ignored or ErlangBooleanAtoms.False })
                return Down(context, node);
            if (result is not Atom { Name: ErlangBooleanAtoms.True })
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), result));
            if (context.Runtime.DistributionPending.TryMonitorNode458(
                context,
                node,
                true,
                passive
            ))
                return Term.A(ErlangBooleanAtoms.True);
            await context.ReduceAsync();
        }
    }

    private static async ValueTask<Term> MonitorTrap(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[2] is Nil)
            return Down(context, arguments[0]);
        Term passive = await context.Runtime.Modules.Call(
            context,
            ListModuleNames.Module,
            ListModuleNames.Member,
            Term.A(RuntimeNodeControlNames.AllowPassive),
            arguments[2]
        );
        if (passive.Equals(Term.A(ErlangBooleanAtoms.False)))
            return Down(context, arguments[0]);
        Term result = await RuntimeNodeKernelRequest.Call(context, Term.Tuple(Term.A(RuntimeNodeControlNames.PassiveRequest), arguments[0]));
        if (result is Atom { Name: RuntimeNodeControlNames.Ignored or ErlangBooleanAtoms.False })
            return Down(context, arguments[0]);
        if (result is not Atom { Name: ErlangBooleanAtoms.True })
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), result));

        return await Monitor(context, arguments);
    }

    private static Term Down(ProcessContext context, Term node)
    {
        context.Runtime.Send(context.Self, Term.Tuple(Term.A(RuntimeNodeControlNames.NodeDown), node));

        return Term.A(ErlangBooleanAtoms.True);
    }

    private static ErlangException Invalid(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        string reason,
        bool badOption
    )
    {
        var entries = new List<KeyValuePair<Term, Term>> { new(Term.A(RuntimeNodeControlNames.ErrorModule), Term.A(RuntimeNodeControlNames.ErrorFormatter)) };
        if (badOption)
            entries.Add(new(Term.A(RuntimeNodeControlNames.Cause), Term.A(RuntimeNodeControlNames.BadOption)));

        return new ErlangException(reason)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), new MapTerm(entries), RuntimeNodeControlNames.Monitor)
        };
    }
}
