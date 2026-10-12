namespace Erlang;

internal static class ProcessSuspensionRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 1, 2 })
            registry.Register(
                ErlangModuleNames.Module,
                ProcessSuspensionNames.Suspend,
                arity,
                Suspend
            );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessSuspensionNames.Resume,
            1,
            (context, arguments) =>
        {
            if (arguments[0] is not Pid { Creation: 0 } pid || pid.Node != context.Runtime.Node || pid.Equals(context.Self))
                throw Invalid(
                    context,
                    arguments,
                    ProcessSuspensionNames.Resume,
                    false
                );
            try
            {
                return ValueTask.FromResult(context.Runtime.Suspensions.Resume(context, pid));
            }
            catch (ErlangException)
            {
                throw Invalid(
                    context,
                    arguments,
                    ProcessSuspensionNames.Resume,
                    false
                );
            }
        }
        );
    }

    private static async ValueTask<Term> Suspend(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Pid { Creation: 0 } pid || pid.Node != context.Runtime.Node || pid.Equals(context.Self))
            throw Invalid(
                context,
                arguments,
                ProcessSuspensionNames.Suspend,
                false
            );
        ProcessSuspensionOptions options;
        try
        {
            options = ProcessSuspensionOptions.Parse(arguments.Count == 2 ? arguments[1] : Nil.Value);
        }
        catch (ErlangException)
        {
            throw Invalid(
                context,
                arguments,
                ProcessSuspensionNames.Suspend,
                true
            );
        }
        try
        {
            return await context.Runtime.Suspensions.Suspend(context, pid, options);
        }
        catch (ErlangException exception)
        {
            throw Invalid(
                context,
                arguments,
                ProcessSuspensionNames.Suspend,
                false,
                exception.Reason
            );
        }
    }

    private static ErlangException Invalid(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        string function,
        bool badOption,
        Term? reason = null
    )
    {
        var entries = new List<KeyValuePair<Term, Term>>
        {
            new(Term.A(ProcessSuspensionNames.ErrorModule), Term.A(ProcessSuspensionNames.ErrorFormatter))
        };
        if (badOption)
            entries.Add(new(Term.A(ProcessSuspensionNames.ErrorCause), Term.A(ProcessSuspensionNames.BadOption)));

        return new ErlangException(reason ?? Term.A(ErlangErrorReasons.BadArgument))
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), new MapTerm(entries), function)
        };
    }
}
