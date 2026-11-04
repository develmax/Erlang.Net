namespace Erlang;

internal static class FileSource
{
    internal static IDisposable Scope(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            FileModule.Name,
            function,
            arity,
            Location(line),
            new object()
        );

    private static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(FileProtocol.SourceLocationFile), Term.String(FileProtocol.SourceFile)),
        Term.Tuple(Term.A(FileProtocol.SourceLocationLine), Term.I(line))
    );

    internal static ErlangException Clause(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        int line
    )
        => Error(
            context,
            function,
            arguments,
            Term.A(ErlangErrorReasons.FunctionClause),
            line
        );

    internal static ErlangException Error(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        Term reason,
        int line,
        int? arity = null
    )
    {
        Term location = Location(line);
        var frames = new List<Term> { Term.Tuple(
            Term.A(FileModule.Name),
            Term.A(function),
            arity is null ? Cons.From(arguments) : Term.I(arity.Value),
            location
        ) };
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        object? previous = null;
        while (caller is not null && frames.Count < Math.Max(FileSourceLimits.MinimumCapturedFrames, context.Runtime.BacktraceDepth))
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

        return new ErlangException(reason)
        {
            StackTraceTerm = Cons.From(frames),
            ExplicitStackTrace = true
        };
    }
}
