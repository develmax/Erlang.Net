namespace Erlang;

internal static class EtsSource
{
    internal static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(EtsSourceMetadata.FileKey), Term.String(EtsSourceMetadata.FileName)),
        Term.Tuple(Term.A(EtsSourceMetadata.LineKey), Term.I(line))
    );

    internal static IDisposable Frame(
        ProcessContext context,
        string function,
        int arity,
        int line,
        bool tail = false
    )
        => context.EnterGeneratedFunction(
            EtsModule.Name,
            function,
            arity,
            Location(line),
            new object(),
            tail
        );

    internal static ErlangException Error(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        Term reason,
        int line,
        Term? info = null
    )
    {
        Term location = info is null ? Location(line) : Cons.From(Cons.Items(Location(line)), Term.List(info));
        var frames = new List<Term>
        {
            Term.Tuple(
                Term.A(EtsModule.Name),
                Term.A(function),
                Cons.From(arguments),
                location
            )
        };
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        if (caller is not null && caller.Module == EtsModule.Name && caller.Function == function)
        {
            caller = caller.Parent;
        }
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
