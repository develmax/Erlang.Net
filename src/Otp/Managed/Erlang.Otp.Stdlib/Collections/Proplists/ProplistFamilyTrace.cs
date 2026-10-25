using System.Collections.Concurrent;

namespace Erlang;

internal static class ProplistFamilyTrace
{

    private static readonly ConcurrentDictionary<int, Term> locations = new();

    private static readonly ConcurrentDictionary<(
        string Name,
        int Arity,
        int Line
    ), Term> returnFrames = new();

    private static readonly Term sourceFile = Cons.From(ProplistFamilySource.FileName.Select(value => Term.I(value)));

    internal static Term Location(int line) => locations.GetOrAdd(
        line,
        value => Term.List(Term.Tuple(Term.A(ProplistFamilySource.File), sourceFile), Term.Tuple(Term.A(ProplistFamilySource.Line), Term.I(value)))
    );

    internal static Term Frame(
        string name,
        Term arguments,
        int line
    ) => Term.Tuple(
        Term.A(ProplistFamilyNames.Module),
        Term.A(name),
        arguments,
        Location(line)
    );

    internal static ProplistFamilyFrame? Push(
        ProcessContext context,
        string name,
        int arity,
        int line,
        object continuationIdentity,
        ProplistFamilyFrame? parent = null
    )
    {
        int limit = BacktraceDepthParameters.MaximumDepth - 1;
        if (parent is not null && ReferenceEquals(parent.ContinuationIdentities[0], continuationIdentity))
            return parent;
        int count = Math.Min(limit, 1 + (parent?.Values.Length ?? 0));
        var values = new Term[count];
        var identities = new object[count];
        values[0] = returnFrames.GetOrAdd((
            name,
            arity,
            line
        ), key => Frame(
            key.Name,
            Term.I(key.Arity),
            key.Line
        ));
        identities[0] = continuationIdentity;
        if (parent is not null)
        {
            Array.Copy(
                parent.Values,
                0,
                values,
                1,
                count - 1
            );
            Array.Copy(
                parent.ContinuationIdentities,
                0,
                identities,
                1,
                count - 1
            );
        }

        return new ProplistFamilyFrame(values, identities);
    }

    internal static ErlangException Clause(
        ProcessContext context,
        string name,
        IReadOnlyList<Term> arguments,
        int line,
        ProplistFamilyFrame? pending = null
    ) => Capture(
        context,
        new ErlangException(ErlangErrorReasons.FunctionClause),
        Frame(
        name,
        Cons.From(arguments),
        line
    ),
        pending
    );

    internal static ErlangException Generator(
        ProcessContext context,
        string name,
        int arity,
        Term tail,
        int line,
        ProplistFamilyFrame? pending = null
    ) => Capture(
        context,
        new ErlangException(Term.Tuple(Term.A(ProplistFamilyNames.BadGenerator), tail)),
        Frame(
        name,
        Term.I(arity),
        line
    ),
        pending
    );

    internal static ErlangException Append(
        ProcessContext context,
        Term prefix,
        Term tail,
        ProplistFamilyFrame? pending
    )
    {
        Term metadata = Term.List(Term.Tuple(
            Term.A(RuntimeExceptionNames.ErrorInfo),
            new MapTerm(new Dictionary<Term, Term> { [Term.A(RuntimeExceptionNames.Module)] = Term.A(RuntimeExceptionNames.Formatter(ErlangModuleNames.Module)!) })
        ));
        Term frame = Term.Tuple(
            Term.A(ErlangModuleNames.Module),
            Term.A(ProplistFamilySource.AppendOperator),
            Term.List(prefix, tail),
            metadata
        );

        return Capture(
            context,
            new ErlangException(ErlangErrorReasons.BadArgument),
            frame,
            pending
        );
    }

    internal static ErlangException Capture(
        ProcessContext context,
        ErlangException error,
        Term first,
        ProplistFamilyFrame? pending = null
    )
    {
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        var frames = new List<Term>
        {
            first
        };
        if (pending is not null)
        {
            foreach (Term frame in pending.Values)
            {
                if (frames.Count >= depth)
                    break;
                frames.Add(frame);
            }
        }

        var caller = context.CaptureExecutionChain();
        if (caller is { SourceWrapper: true, Module: ProplistFamilyNames.Module })
            caller = caller.Parent;
        object? previousContinuation = pending is not null ? pending.ContinuationIdentities[^1] : null;
        while (caller is not null && frames.Count < depth)
        {
            object? continuation = caller.ContinuationIdentity;
            if (continuation is not null && ReferenceEquals(previousContinuation, continuation))
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
            previousContinuation = continuation;
            caller = caller.Parent;
        }

        return error.WithCapturedStack(Cons.From(frames));
    }
}
