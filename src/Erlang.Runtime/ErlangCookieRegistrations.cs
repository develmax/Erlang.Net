namespace Erlang;

internal static class ErlangCookieRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 0, 1 })
            registry.Register(
                ErlangModuleNames.Module,
                ErlangCookieNames.Get,
                arity,
                Get
            );
        foreach (int arity in new[] { 1, 2 })
            registry.Register(
                ErlangModuleNames.Module,
                ErlangCookieNames.Set,
                arity,
                Set
            );
    }

    private static ValueTask<Term> Get(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments.Count == 1 && arguments[0] is not Atom)
            throw Invalid(context, arguments, ErlangCookieNames.Get);
        Term auth = context.Runtime.WhereIs(ErlangCookieNames.Auth);
        if (auth is not Pid)
            return ValueTask.FromResult<Term>(Term.A(ErlangCookieNames.NoCookie));
        Term request = arguments.Count == 0 || arguments[0].Equals(context.Runtime.NodeAtom)
            ? Term.A(ErlangCookieNames.GetOwn)
            : Term.Tuple(Term.A(ErlangCookieNames.Get), arguments[0]);

        return Call(context, auth, request);
    }

    private static ValueTask<Term> Set(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments.Any(argument => argument is not Atom) || arguments.Count == 2 && arguments[0].Equals(Term.A(ProcessNodeDefaults.Local)))
            throw Invalid(context, arguments, ErlangCookieNames.Set);
        Term auth = context.Runtime.WhereIs(ErlangCookieNames.Auth);
        bool own = arguments.Count == 1 || arguments[0].Equals(context.Runtime.NodeAtom);
        if (auth is not Pid)
        {
            using var frame = context.EnterFunction(ErlangCookieNames.Auth, ErlangCookieNames.Set, own ? 1 : 2);
            Term errorArguments = own ? Term.List(arguments[^1]) : Cons.From(arguments);
            throw new ErlangException(ErlangCookieNames.NotStarted)
            {
                ExplicitStackTrace = true,
                StackTraceTerm = context.CaptureErrorStack(errorArguments, null)
            };
        }
        Term request = own
            ? Term.Tuple(Term.A(ErlangCookieNames.SetOwn), arguments[^1])
            : Term.Tuple(Term.A(ErlangCookieNames.Set), arguments[0], arguments[1]);

        return Call(context, auth, request);
    }

    private static ValueTask<Term> Call(ProcessContext context, Term auth, Term request) => context.Runtime.Modules.Call(
        context,
        ErlangCookieNames.Server,
        ErlangCookieNames.Call,
        auth,
        request,
        Term.A(ErlangCookieNames.Infinity)
    );

    private static ErlangException Invalid(ProcessContext context, IReadOnlyList<Term> arguments, string function)
    {
        var info = new MapTerm([new(Term.A(ErlangCookieNames.ErrorModule), Term.A(ErlangCookieNames.ErrorFormatter))]);

        return new ErlangException(ErlangErrorReasons.BadArgument)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), info, function)
        };
    }
}
