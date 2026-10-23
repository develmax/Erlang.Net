namespace Erlang;

internal static class ProcessDisplayRegistrations
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        ProcessDisplayNames.Function,
        2,
        async (context, arguments) =>
        {
            if (arguments[1] is not Atom { Name: ProcessDisplayNames.Backtrace })
                throw Invalid(context, arguments, true);
            if (arguments[0] is not Pid pid || pid.Node != context.Runtime.Node || pid.Creation != context.Runtime.InitializationPid.Creation)
                throw Invalid(context, arguments, false);
            Term result = await context.Runtime.DisplayProcess(context, pid);
            context.Cancellation.ThrowIfCancellationRequested();
            if (result.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                throw Invalid(context, arguments, false);

            return result;
        }
    );

    private static ErlangException Invalid(ProcessContext context, IReadOnlyList<Term> arguments, bool badOption)
    {
        var fields = new List<KeyValuePair<Term, Term>>
        {
            new(Term.A(ProcessDisplayNames.ErrorModule), Term.A(ProcessDisplayNames.ErrorFormatter))
        };
        if (badOption)
            fields.Add(new(Term.A(ProcessDisplayNames.Cause), Term.A(ProcessDisplayNames.BadOption)));

        return new ErlangException(ErlangErrorReasons.BadArgument)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), new MapTerm(fields), ProcessDisplayNames.Function)
        };
    }
}
