namespace Erlang;

internal static class ProcessGarbageCollectionRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 0, 1, 2 })
            registry.Register(
                ErlangModuleNames.Module,
                ProcessGarbageCollectionNames.Function,
                arity,
                Collect
            );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessGarbageCollectionNames.MessageAreaFunction,
            0,
            (_, _) => throw new ErlangException(ProcessGarbageCollectionNames.MessageAreaError)
        );
    }

    private static async ValueTask<Term> Collect(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        ProcessGarbageCollectionOptions options;
        try
        {
            options = ProcessGarbageCollectionOptions.Parse(arguments.Count == 2 ? arguments[1] : Nil.Value);
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ProcessGarbageCollectionNames.BadOption)))
        {
            throw InvalidArguments(context, arguments, true);
        }
        Term target = arguments.Count == 0 ? context.Self : arguments[0];
        if (target is not Pid pid || pid.Node != context.Runtime.Node ||
            options.Type is not Atom { Name: ProcessGarbageCollectionNames.Major or ProcessGarbageCollectionNames.Minor })
            throw InvalidArguments(context, arguments, false);

        return await context.Runtime.CollectProcessGarbage(context, pid, options);
    }

    private static ErlangException InvalidArguments(ProcessContext context, IReadOnlyList<Term> arguments, bool badOption)
    {
        var entries = new List<KeyValuePair<Term, Term>>
        {
            new(Term.A(ProcessGarbageCollectionNames.ErrorModuleKey), Term.A(ProcessGarbageCollectionNames.ErrorFormatter))
        };
        if (badOption)
            entries.Add(new(Term.A(ProcessGarbageCollectionNames.ErrorCauseKey), Term.A(ProcessGarbageCollectionNames.BadOption)));

        return new ErlangException(ErlangErrorReasons.BadArgument)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), new MapTerm(entries), ProcessGarbageCollectionNames.Function)
        };
    }
}
