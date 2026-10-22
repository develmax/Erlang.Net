using System.Collections.Concurrent;

namespace Erlang;

internal static class QueueCoreTrace
{
    private static readonly ConcurrentDictionary<int, Term> locations = new();
    private static readonly Term fileName = Term.String(QueueCoreSource.FileName);

    internal static ErlangException Public(
        ProcessContext context,
        string name,
        IReadOnlyList<Term> arguments,
        int line
    )
        => Capture(context, Frame(name, Cons.From(arguments), line), name);

    internal static ErlangException Dependency(
        ProcessContext context,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        string owner,
        int line,
        int ownerArity = 1,
        bool includeOwner = true
    )
    {
        string? formatter = RuntimeExceptionNames.Formatter(module);
        Term metadata = formatter is null ? Nil.Value : Term.List(Term.Tuple(
            Term.A(RuntimeExceptionNames.ErrorInfo),
            new MapTerm(new Dictionary<Term, Term>
            {
                [Term.A(RuntimeExceptionNames.Module)] = Term.A(formatter)
            })
        ));
        Term first = Term.Tuple(
            Term.A(module),
            Term.A(function),
            Cons.From(arguments),
            metadata
        );

        return Capture(
            context,
            first,
            owner,
            includeOwner ? Frame(owner, Term.I(ownerArity), line) : null
        );
    }

    private static Term Frame(string name, Term arguments, int line) => Term.Tuple(
        Term.A(QueueModuleNames.Module),
        Term.A(name),
        arguments,
        locations.GetOrAdd(
            line,
            value => Term.List(
            Term.Tuple(Term.A(QueueCoreSource.File), fileName),
            Term.Tuple(Term.A(QueueCoreSource.Line), Term.I(value))
        )
        )
    );

    private static ErlangException Capture(
        ProcessContext context,
        Term first,
        string sourceOwner,
        Term? owner = null
    )
    {
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        var frames = new List<Term> { first };
        if (owner is not null && frames.Count < depth)
            frames.Add(owner);
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        if (caller is { SourceWrapper: true, Module: QueueModuleNames.Module }
            && (caller.Function == sourceOwner || QueueTailAliases.Matches(caller.Function, sourceOwner)))
            caller = caller.Parent;
        object? previousContinuation = null;
        while (caller is not null && frames.Count < depth)
        {
            if (caller.ContinuationIdentity is { } continuation && ReferenceEquals(continuation, previousContinuation))
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
            previousContinuation = caller.ContinuationIdentity;
            caller = caller.Parent;
        }

        return new ErlangException(ErlangErrorReasons.BadArgument).WithCapturedStack(Cons.From(frames));
    }
}
