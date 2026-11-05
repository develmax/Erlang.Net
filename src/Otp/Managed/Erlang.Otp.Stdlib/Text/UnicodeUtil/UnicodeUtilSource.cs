using System.Collections.Concurrent;

namespace Erlang;

internal static class UnicodeUtilSource
{
    private static readonly ConcurrentDictionary<(string Function, int Arity, int Line), object> Continuations = new();
    private static readonly ConcurrentDictionary<int, Term> Locations = new();

    internal static IDisposable Scope(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            UnicodeUtilNames.Module,
            function,
            arity,
            Location(line),
            Continuations.GetOrAdd((function, arity, line), static _ => new object())
        );

    internal static ErlangException Clause(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> args,
        int line
    )
        => Capture(
            context,
            Term.A(ErlangErrorReasons.FunctionClause),
            function,
            Cons.From(args),
            line
        );

    internal static ErlangException BadArgument(
        ProcessContext context,
        string function,
        Term reason,
        int line
    )
        => Capture(
            context,
            reason,
            function,
            Term.I(1),
            line
        );

    private static ErlangException Capture(
        ProcessContext context,
        Term reason,
        string function,
        Term args,
        int line
    )
    {
        var frames = new List<Term> { Term.Tuple(
            Term.A(UnicodeUtilNames.Module),
            Term.A(function),
            args,
            Location(line)
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
                caller.HostHandlerArguments is { } arguments ? Cons.From(arguments) : Term.I(caller.Arity),
                caller.HostHandlerArguments is not null ? Nil.Value : caller.SourceLocation ?? Nil.Value
            ));
            previous = caller.ContinuationIdentity;
            caller = caller.Parent;
        }

        return new ErlangException(reason).WithCapturedStack(Cons.From(frames));
    }

    private static Term Location(int line) => Locations.GetOrAdd(
        line,
        static value => Term.List(
        Term.Tuple(Term.A("file"), Term.String(UnicodeUtilNames.SourceFile)), Term.Tuple(Term.A("line"), Term.I(value)))
    );
}
