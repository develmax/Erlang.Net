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
                        StackTraceTerm = context.CaptureErrorStack(
                            Cons.From(arguments),
                            new MapTerm(entries),
                            function,
                            function == PortNames.Open ? PortErrorSourceLocations.Open(exception.Reason) : null
                        )
                    };
                }
            }
        );
        Add(
            PortNames.Open,
            2,
            (context, arguments) => context.Runtime.OpenConfiguredPort422(context, arguments[0], arguments[1])
        );
        Add(PortNames.Ports, 0, (context, _) => ValueTask.FromResult(context.Runtime.Ports.Snapshot()));
        Add(PortNames.Close, 1, (context, arguments) => context.Runtime.Ports.CloseOrdered(arguments[0], context.Cancellation, context));
        Add(
            PortNames.Connect,
            2,
            (context, arguments) => context.Runtime.Ports.ConnectOrdered455(context, arguments[0], arguments[1])
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
            return await context.Runtime.Ports.InfoAsync454(context, arguments[0], null);
        }
        );
        Add(
            PortNames.Info,
            2,
            async (context, arguments) =>
        {
            return await context.Runtime.Ports.InfoAsync454(context, arguments[0], arguments[1]);
        }
        );
        Add(PortNames.Command, 2, (context, arguments) => context.Runtime.Ports.Command(
            arguments[0],
            arguments[1],
            Nil.Value,
            context
        ));
        Add(PortNames.Command, 3, (context, arguments) => context.Runtime.Ports.Command(
            arguments[0],
            arguments[1],
            arguments[2],
            context
        ));

        Add(PortNames.Call, 2, (context, arguments) => context.Runtime.DriverCall422(
            context,
            arguments[0],
            Term.I(0),
            arguments[1]
        ));
        Add(PortNames.Call, 3, (context, arguments) => context.Runtime.DriverCall422(
            context,
            arguments[0],
            arguments[1],
            arguments[2]
        ));
        Add(
            PortNames.Control,
            3,
            (context, arguments) => context.Runtime.DriverControl422(
                context,
                arguments[0],
                arguments[1],
                arguments[2]
            )
        );
    }
}
