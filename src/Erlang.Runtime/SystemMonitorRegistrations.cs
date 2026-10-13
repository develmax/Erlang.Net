namespace Erlang;

internal static class SystemMonitorRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        for (int arity = 0; arity <= 2; arity++)
            registry.Register(
                ErlangModuleNames.Module,
                SystemMonitorNames.Function,
                arity,
                Invoke
            );
    }

    private static ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        try
        {
            Term result = arguments.Count switch
            {
                0 => context.Runtime.SystemMonitoring.Get(),
                1 when arguments[0] is Atom { Name: SystemMonitorNames.Undefined } => context.Runtime.SystemMonitoring.Configure(arguments[0], Nil.Value),
                1 when arguments[0] is TupleTerm { Items: [var target, var options] } => context.Runtime.SystemMonitoring.Configure(target, options),
                2 => context.Runtime.SystemMonitoring.Configure(arguments[0], arguments[1]),
                _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
            };

            return ValueTask.FromResult(result);
        }
        catch (ErlangException exception)
        {
            throw new ErlangException(exception.Reason)
            {
                ExplicitStackTrace = true,
                StackTraceTerm = context.CaptureErrorStack(
                    Cons.From(arguments),
                    new MapTerm([new KeyValuePair<Term, Term>(Term.A(SystemMonitorNames.ErrorModule), Term.A(SystemMonitorNames.ErrorFormatter))]),
                    SystemMonitorNames.Function
                )
            };
        }
    }
}
