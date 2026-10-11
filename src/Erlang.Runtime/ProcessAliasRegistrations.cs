namespace Erlang;

internal static class ProcessAliasRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, int arity, Func<ProcessContext, IReadOnlyList<Term>, Term> body) => registry.Register(
            ErlangModuleNames.Module,
            name,
            arity,
            (context, arguments) => ValueTask.FromResult(body(context, arguments))
        );
        Add(ProcessAliasNames.Alias, 0, (context, _) => context.Runtime.CreateAlias(context, false, false));
        Add(
            ProcessAliasNames.Alias,
            1,
            (context, arguments) =>
        {
            bool reply = false, priority = false;
            foreach (Term option in Cons.Items(arguments[0]))
            {
                switch (option)
                {
                    case Atom { Name: ProcessAliasNames.Explicit }:
                        reply = false;
                        break;
                    case Atom { Name: ProcessAliasNames.Reply }:
                        reply = true;
                        break;
                    case Atom { Name: ProcessAliasNames.Priority }:
                        priority = true;
                        break;
                    default:
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
            }

            return context.Runtime.CreateAlias(context, reply, priority);
        }
        );
        Add(
            ProcessAliasNames.Unalias,
            1,
            (context, arguments) => arguments[0] is ReferenceTerm reference
            ? CoreModules.Boolean(context.Runtime.Unalias(context, reference))
            : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(ErlangBasicBifNames.Send, 3, (context, arguments) => Send(
            context,
            arguments[0],
            arguments[1],
            arguments[2]
        ));
        Add(
            ProcessSendNames.NoSuspend,
            2,
            (context, arguments) => CoreModules.Boolean(Send(
                context,
                arguments[0],
                arguments[1],
                Nil.Value
            ).Equals(Term.A(CommonAtomNames.Ok)))
        );
        Add(
            ProcessSendNames.NoSuspend,
            3,
            (context, arguments) => CoreModules.Boolean(Send(
                context,
                arguments[0],
                arguments[1],
                arguments[2]
            ).Equals(Term.A(CommonAtomNames.Ok)))
        );
    }

    private static Term Send(
        ProcessContext context,
        Term target,
        Term message,
        Term options
    )
    {
        bool priority = false, noConnect = false;
        foreach (Term option in Cons.Items(options))
        {
            switch (option)
            {
                case Atom { Name: ProcessSendOptionNames.Priority }:
                    priority = true;
                    break;
                case Atom { Name: ProcessSendOptionNames.NoConnect }:
                    noConnect = true;
                    break;
                case Atom { Name: ProcessSendOptionNames.NoSuspend }:
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
        if (noConnect && (target is Pid remotePid && remotePid.Node != context.Runtime.Node || target is ReferenceTerm remoteReference && remoteReference.Node != context.Runtime.Node || target is TupleTerm { Items.Count: 2 } address && address.Items[0] is Atom && address.Items[1] is Atom remoteNode && remoteNode.Name != context.Runtime.Node))
            return Term.A(ProcessSendResultNames.NoConnect);
        ErlangOperatorValues.Send(
            context,
            target,
            message,
            priority
        );

        return Term.A(CommonAtomNames.Ok);
    }
}
