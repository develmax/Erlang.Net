namespace Erlang;

internal static class ProcessCodeRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 2, 3 })
            registry.Register(
                ErlangModuleNames.Module,
                ProcessCodeCheckNames.Function,
                arity,
                Check
            );
    }

    private static async ValueTask<Term> Check(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        ProcessCodeOptions options;
        try
        {
            options = ProcessCodeOptions.Parse(arguments.Count == 3 ? arguments[2] : Nil.Value);
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ProcessCodeCheckNames.BadOption)))
        {
            throw InvalidArguments(context, arguments, true);
        }
        if (arguments[0] is not Pid pid || pid.Node != context.Runtime.Node || arguments[1] is not Atom module)
            throw InvalidArguments(context, arguments, false);

        return await context.Runtime.CheckProcessCode(
            context,
            pid,
            module.Name,
            options.Async,
            options.RequestId
        );
    }

    private static ErlangException InvalidArguments(ProcessContext context, IReadOnlyList<Term> arguments, bool badOption)
    {
        var entries = new List<KeyValuePair<Term, Term>>
        {
            new(Term.A(ProcessCodeCheckNames.ErrorModuleKey), Term.A(ProcessCodeCheckNames.ErrorFormatter))
        };
        if (badOption)
            entries.Add(new(Term.A(ProcessCodeCheckNames.ErrorCauseKey), Term.A(ProcessCodeCheckNames.BadOption)));

        return new ErlangException(ErlangErrorReasons.BadArgument)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), new MapTerm(entries), ProcessCodeCheckNames.Function)
        };
    }
}
