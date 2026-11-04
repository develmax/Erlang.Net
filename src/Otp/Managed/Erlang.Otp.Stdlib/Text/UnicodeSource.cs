namespace Erlang;

internal static class UnicodeSource
{
    internal static ErlangException Error(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        Term reason,
        bool badarg = false,
        bool fake = false
    )
    {
        Term info = Term.Tuple(Term.A("error_info"), new MapTerm([new KeyValuePair<Term, Term>(Term.A("module"), Term.A("erl_stdlib_errors"))]));
        Term location = fake ? Term.List(info) : Term.List(
            Term.Tuple(Term.A("file"), Term.String(UnicodeNames.SourceFile)),
            Term.Tuple(Term.A("line"), Term.I(badarg ? 1373 : 1376)),
            info
        );

        return Capture(
            context,
            function,
            arguments,
            reason,
            location
        );
    }

    internal static ErlangException Clause(
        ProcessContext context,
        string function,
        Term argument,
        int line
    )
        => Capture(
            context,
            function,
            [argument],
            Term.A(ErlangErrorReasons.FunctionClause),
            Term.List(
            Term.Tuple(Term.A("file"), Term.String(UnicodeNames.SourceFile)),
            Term.Tuple(Term.A("line"), Term.I(line)))
        );

    private static ErlangException Capture(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        Term reason,
        Term location
    )
    {
        var frames = new List<Term> { Term.Tuple(
            Term.A(UnicodeModule.Name),
            Term.A(function),
            Cons.From(arguments),
            location
        ) };
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        object? previous = null;
        while (caller is not null && frames.Count < Math.Max(1, context.Runtime.BacktraceDepth))
        {
            if (caller.ContinuationIdentity is { } identity && ReferenceEquals(previous, identity))
            {
                caller = caller.Parent;
                continue;
            }
            frames.Add(Term.Tuple(
                Term.A(caller.Module),
                Term.A(caller.Function),
                caller.HostHandlerArguments is { } args ? Cons.From(args) : Term.I(caller.Arity),
                caller.HostHandlerArguments is not null ? Nil.Value : caller.SourceLocation ?? Nil.Value
            ));
            previous = caller.ContinuationIdentity;
            caller = caller.Parent;
        }

        return new ErlangException(reason).WithCapturedStack(Cons.From(frames));
    }
}
