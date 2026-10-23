namespace Erlang;

public static class HaltRegistrations
{
    public static void Register(ModuleRegistry modules)
    {
        for (int arity = 0; arity <= 2; arity++)
            modules.Register(
                ErlangModuleNames.Module,
                HaltNames.Function,
                arity,
                Execute
            );
    }

    private static ValueTask<Term> Execute(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        RuntimeHaltRequest request;
        try
        {
            request = HaltRequestParser.Read(arguments.Count == 0 ? Term.I(0) : arguments[0], arguments.Count == 2 ? arguments[1] : Nil.Value);
        }
        catch (HaltParseException error)
        {
            var fields = new List<KeyValuePair<Term, Term>>
            {
                new(Term.A(HaltNames.ErrorModule), Term.A(HaltNames.ErrorFormatter))
            };
            if (arguments.Count == 2 && error.BadOptions)
                fields.Add(new(Term.A(HaltNames.Cause), Term.A(HaltNames.BadOption)));
            var info = new MapTerm(fields);
            throw new ErlangException(ErlangErrorReasons.BadArgument)
            {
                ExplicitStackTrace = true,
                StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), info, HaltNames.Function)
            };
        }
        if (request.Kind == RuntimeHaltKind.CrashDump)
            request = request with { LogicalStack = context.CaptureErrorStack(Cons.From(arguments), null, HaltNames.Function) };
        context.Runtime.RequestHalt(request);
        throw new ProcessHaltTransfer(context.Runtime);
    }
}
