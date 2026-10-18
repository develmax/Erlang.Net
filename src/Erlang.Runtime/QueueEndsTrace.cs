namespace Erlang;

internal static class QueueEndsTrace
{
    internal static ErlangException Empty(
        ProcessContext context,
        string owner,
        Term queue,
        int line
    )
        => Capture(context, QueueModuleNames.Empty, Frame(
            QueueModuleNames.Module,
            owner,
            Cons.From([queue]),
            QueueCoreSource.FileName,
            line
        ));

    internal static ErlangException LastClause(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        int sourceLine,
        string? owner,
        int line
    )
        => Capture(
            context,
            ErlangErrorReasons.FunctionClause,
            Frame(
                ListModuleNames.Module,
                ListModuleNames.Last,
                Cons.From(arguments),
                QueueEndsSource.ListsFileName,
                sourceLine
            ),
            owner is null ? null : Frame(
                QueueModuleNames.Module,
                owner,
                Term.I(1),
                QueueCoreSource.FileName,
                line
            )
        );

    private static Term Frame(
        string module,
        string name,
        Term arguments,
        string file,
        int line
    )
        => Term.Tuple(
            Term.A(module),
            Term.A(name),
            arguments,
            Term.List(
            Term.Tuple(Term.A(QueueCoreSource.File), Term.String(file)),
            Term.Tuple(Term.A(QueueCoreSource.Line), Term.I(line)))
        );

    private static ErlangException Capture(
        ProcessContext context,
        string reason,
        Term first,
        Term? pending = null
    )
    {
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        var frames = new List<Term> { first };
        if (pending is not null && frames.Count < depth)
            frames.Add(pending);
        var caller = context.CaptureExecutionChain();
        if (caller is { SourceWrapper: true, Module: QueueModuleNames.Module })
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

        return new ErlangException(reason).WithCapturedStack(Cons.From(frames));
    }
}
