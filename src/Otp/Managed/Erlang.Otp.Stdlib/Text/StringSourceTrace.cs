using System.Collections.Concurrent;

namespace Erlang;

internal static class StringSourceTrace
{
    private static readonly ConcurrentDictionary<(string Function, int Arity, int Line), object> Continuations = new();

    internal static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(StringSourceNames.File)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
    );

    internal static IDisposable Scope(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            StringModule.Name,
            function,
            arity,
            Location(line),
            Continuations.GetOrAdd((function, arity, line), static _ => new object())
        );

    internal static Term Frame(string function, int arity, int line)
        => Term.Tuple(
            Term.A(StringModule.Name),
            Term.A(function),
            Term.I(arity),
            Location(line)
        );

    internal static ErlangException Clause(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        int line,
        params Term[] pending
    )
        => Capture(
            context,
            new ErlangException(ErlangErrorReasons.FunctionClause),
            Term.Tuple(
                Term.A(StringModule.Name),
                Term.A(function),
                Cons.From(arguments),
                Location(line)
            ),
            pending
        );

    internal static ErlangException Error(
        ProcessContext context,
        Term reason,
        string function,
        int arity,
        int line,
        params Term[] pending
    )
        => Capture(
            context,
            new ErlangException(reason),
            Frame(function, arity, line),
            pending
        );

    private static ErlangException Capture(
        ProcessContext context,
        ErlangException error,
        Term origin,
        IReadOnlyList<Term> pending
    )
    {
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        var frames = new List<Term> { origin };
        foreach (Term frame in pending)
        {
            if (frames.Count == depth)
            {
                break;
            }
            frames.Add(frame);
        }
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        object? previous = null;
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
                caller.HostHandlerArguments is { } arguments ? Cons.From(arguments) : Term.I(caller.Arity),
                caller.HostHandlerArguments is not null ? Nil.Value : caller.SourceLocation ?? Nil.Value
            ));
            previous = caller.ContinuationIdentity;
            caller = caller.Parent;
        }

        return error.WithCapturedStack(Cons.From(frames));
    }
}
