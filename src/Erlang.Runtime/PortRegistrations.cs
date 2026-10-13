namespace Erlang;

internal static class PortRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string function, int arity, Func<ProcessContext, IReadOnlyList<Term>, ValueTask<Term>> body) => registry.Register(
            ErlangModuleNames.Module,
            function,
            arity,
            async (context, arguments) =>
            {
                try
                {
                    Term result = await body(context, arguments);
                    context.Runtime.AdmitSignals(context);

                    return result;
                }
                catch (ErlangException exception) when (!exception.ExplicitStackTrace)
                {
                    if (function is PortNames.GetData or PortNames.SetData or PortNames.Ports)
                        throw;
                    var entries = new List<KeyValuePair<Term, Term>> { new(Term.A(PortNames.ErrorModule), Term.A(PortNames.ErrorFormatter)) };
                    Term reason = exception.Reason;
                    if (reason is Atom { Name: PortNames.BadOption or PortNames.BadType })
                    {
                        if (function != PortNames.Info || arity == 2)
                            entries.Add(new(Term.A(PortNames.Cause), reason));
                        reason = Term.A(ErlangErrorReasons.BadArgument);
                    }
                    throw new ErlangException(reason)
                    {
                        ExplicitStackTrace = true,
                        StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), new MapTerm(entries), function)
                    };
                }
            }
        );
        Add(
            PortNames.Open,
            2,
            (context, arguments) => ValueTask.FromResult(context.Runtime.Ports.Open(context, arguments[0], arguments[1]))
        );
        Add(PortNames.Ports, 0, (context, _) => ValueTask.FromResult(context.Runtime.Ports.Snapshot()));
        Add(PortNames.Close, 1, (context, arguments) => context.Runtime.Ports.CloseOrdered(arguments[0]));
        Add(
            PortNames.Connect,
            2,
            (context, arguments) => ValueTask.FromResult(context.Runtime.Ports.Connect(arguments[0], arguments[1]))
        );
        Add(PortNames.GetData, 1, (context, arguments) => ValueTask.FromResult(context.Runtime.Ports.GetData(arguments[0])));
        Add(
            PortNames.SetData,
            2,
            (context, arguments) => ValueTask.FromResult(context.Runtime.Ports.SetData(arguments[0], arguments[1]))
        );
        Add(
            PortNames.Info,
            1,
            async (context, arguments) =>
        {
            await context.Runtime.Ports.AwaitCommands(arguments[0]);

            return context.Runtime.Ports.Info(arguments[0], null);
        }
        );
        Add(
            PortNames.Info,
            2,
            async (context, arguments) =>
        {
            await context.Runtime.Ports.AwaitCommands(arguments[0]);

            return context.Runtime.Ports.Info(arguments[0], arguments[1]);
        }
        );
        Add(PortNames.Command, 2, (context, arguments) => context.Runtime.Ports.Command(arguments[0], arguments[1], Nil.Value));
        Add(PortNames.Command, 3, (context, arguments) => context.Runtime.Ports.Command(arguments[0], arguments[1], arguments[2]));

        // The spawned-process driver has neither a call nor a control callback.
        ValueTask<Term> Unsupported(ProcessContext context, IReadOnlyList<Term> arguments) => throw new ErlangException(ErlangErrorReasons.BadArgument);
        Add(PortNames.Call, 2, Unsupported);
        Add(PortNames.Call, 3, Unsupported);
        Add(PortNames.Control, 3, Unsupported);
    }
}
