namespace Erlang;

internal static class ErrorLoggerSource
{
    internal const string File = ErrorLoggerSourceFields.ErrorLoggerErl;

    internal static IDisposable Frame(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            ErrorLoggerModule.Name,
            function,
            arity,
            Location(line),
            new object()
        );

    internal static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(ErrorLoggerSourceFields.File), Term.String(File)),
        Term.Tuple(Term.A(ErrorLoggerSourceFields.Line), Term.I(line))
    );

    internal static ErlangException Error(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        string reason,
        int line
    ) => Capture(
        context,
        Term.A(reason),
        [Term.Tuple(
            Term.A(ErrorLoggerModule.Name),
            Term.A(function),
            Cons.From(arguments),
            Location(line)
        )]
    );

    internal static ErlangException Capture(
        ProcessContext context,
        Term reason,
        IEnumerable<Term> prefix,
        string exceptionClass = ErlangExceptionClasses.Error
    )
    {
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        var frames = prefix.Take(depth).ToList();
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        object? previous = null;
        while (caller is not null && frames.Count < depth)
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

        return new ErlangException(reason, exceptionClass)
        {
            StackTraceTerm = Cons.From(frames),
            ExplicitStackTrace = true
        };
    }
}
