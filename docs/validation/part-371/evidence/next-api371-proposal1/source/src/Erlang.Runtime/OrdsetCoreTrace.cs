namespace Erlang;

internal static class OrdsetCoreTrace
{
    internal static ErlangException Clause(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        int line,
        int pending = 0
    )
    {
        var frames = new List<Term> { Frame(function, Cons.From(arguments), line) };
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        for (int index = 0; index < pending && frames.Count < depth; index++)
            frames.Add(Frame(function, Term.I(arguments.Count), line));

        return Capture(context, ErlangErrorReasons.FunctionClause, frames);
    }

    internal static ErlangException Length(ProcessContext context, Term set)
    {
        Term metadata = Term.List(Term.Tuple(
            Term.A(RuntimeExceptionNames.ErrorInfo),
            new MapTerm(new Dictionary<Term, Term>
            {
                [Term.A(RuntimeExceptionNames.Module)] = Term.A(RuntimeExceptionNames.Formatter(ErlangModuleNames.Module)!)
            })
        ));
        var frames = new List<Term>
        {
            Term.Tuple(
                Term.A(ErlangModuleNames.Module),
                Term.A(OrdsetCoreNames.Length),
                Term.List(set),
                metadata
            )
        };
        if (Math.Max(1, context.Runtime.BacktraceDepth) > frames.Count)
            frames.Add(Frame(OrdsetCoreNames.Size, Term.I(1), OrdsetCoreNames.SizeLine));

        return Capture(context, ErlangErrorReasons.BadArgument, frames);
    }

    private static Term Frame(string function, Term arguments, int line) => Term.Tuple(
        Term.A(OrdsetCoreNames.Module),
        Term.A(function),
        arguments,
        Term.List(
            Term.Tuple(Term.A(OrdsetCoreNames.File), Term.String(OrdsetCoreNames.FileName)),
            Term.Tuple(Term.A(OrdsetCoreNames.Line), Term.I(line))
        )
    );

    private static ErlangException Capture(ProcessContext context, string reason, List<Term> frames)
    {
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        if (caller is { SourceWrapper: true, Module: OrdsetCoreNames.Module })
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
