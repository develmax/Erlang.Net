namespace Erlang;

internal static class ProcessSavedCallRegistrations
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        ErlangModuleNames.ProcessFlag,
        3,
        Change
    );

    public static Term ChangeSelf(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        try
        {
            return Term.I(context.SavedCalls.Configure(arguments[1]));
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            throw Invalid(context, arguments, false);
        }
    }

    private static async ValueTask<Term> Change(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Pid { Creation: 0 } pid || pid.Node != context.Runtime.Node)
            throw Invalid(context, arguments, true);
        Term result = await context.Runtime.ChangeSavedCalls(
            context,
            pid,
            arguments[1],
            arguments[2]
        );
        if (result is Atom { Name: ProcessSavedCallNames.BadType })
            throw Invalid(context, arguments, true);
        if (result is Atom { Name: ErlangErrorReasons.BadArgument })
            throw Invalid(context, arguments, false);

        return result;
    }

    private static ErlangException Invalid(ProcessContext context, IReadOnlyList<Term> arguments, bool badType)
    {
        var entries = new List<KeyValuePair<Term, Term>> { new(Term.A(ProcessSavedCallNames.ErrorModule), Term.A(ProcessSavedCallNames.ErrorFormatter)) };
        if (badType)
            entries.Add(new(Term.A(ProcessSavedCallNames.Cause), Term.A(ProcessSavedCallNames.BadType)));

        return new ErlangException(ErlangErrorReasons.BadArgument)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), new MapTerm(entries), ErlangModuleNames.ProcessFlag)
        };
    }
}
