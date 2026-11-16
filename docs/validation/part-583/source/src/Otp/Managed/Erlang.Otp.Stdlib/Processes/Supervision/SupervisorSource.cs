namespace Erlang;

internal static class SupervisorSource
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string Function, int Line), object> Continuations = new();

    internal static IDisposable Scope(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            SupervisorModule.Name,
            function,
            arity,
            Term.List(
                Term.Tuple(Term.A(SupervisorProtocol.File), Term.String(SupervisorSourceNames.FileName)),
                Term.Tuple(Term.A(SupervisorProtocol.Line), Term.I(line))
            ),
            Continuations.GetOrAdd((function, line), static _ => new object())
        );

    internal static ErlangException FunctionClause(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        int line = 0
    )
    {
        Term location = line == 0 ? Nil.Value : Term.List(
            Term.Tuple(Term.A(SupervisorProtocol.File), Term.String(SupervisorSourceNames.FileName)),
            Term.Tuple(Term.A(SupervisorProtocol.Line), Term.I(line))
        );
        var frames = new List<Term>
        {
            Term.Tuple(
                Term.A(SupervisorModule.Name),
                Term.A(function),
                Cons.From(arguments),
                location
            )
        };
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        object? previous = null;
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        while (caller is not null && frames.Count < depth)
        {
            if (caller.ContinuationIdentity is { } identity && ReferenceEquals(identity, previous))
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

        return new ErlangException(SupervisorProtocol.FunctionClause).WithCapturedStack(Cons.From(frames));
    }
}
