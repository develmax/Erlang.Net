namespace Erlang;

internal static class RuntimeExceptionCapture
{
    internal static ErlangException Source(
        ProcessContext context,
        ErlangException error,
        ErlangExecutionFrame? frame,
        IReadOnlyList<Term>? arguments = null,
        bool selectedHandler = false
    )
    {
        if (error.ExplicitStackTrace || error.AutomaticStackTraceCaptured || frame is null)
            return error;
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        var trace = new List<Term>();
        bool first = true;
        while (frame is not null && trace.Count < depth)
        {
            trace.Add(Term.Tuple(
                Term.A(frame.Module),
                Term.A(frame.Function),
                first && arguments is not null ? Cons.From(arguments)
                    : frame.HostHandlerArguments is { } handlerArguments ? Cons.From(handlerArguments) : Term.I(frame.Arity),
                Nil.Value
            ));
            first = false;
            frame = frame.Parent;
        }

        return error.WithCapturedStack(Cons.From(trace), selectedHandler);
    }

    internal static ErlangException Call(
        ProcessContext context,
        ErlangException error,
        ErlangExecutionFrame? frame,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        bool native,
        Term? sourceLocation = null
    )
    {
        if (error.ExplicitStackTrace || error.AutomaticStackTraceCaptured)
            return error;
        if (native && module == ErlangModuleNames.Module &&
            (function == RuntimeExceptionNames.Error && arguments.Count is 1 or 2 or 3 ||
             function == RuntimeExceptionNames.Exit && arguments.Count == 1 ||
             function == RuntimeExceptionNames.Throw && arguments.Count == 1))
            return Source(context, error, frame);
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        string? formatter = native ? RuntimeExceptionNames.Formatter(module) : null;
        Term location = sourceLocation ?? (formatter is not null
            ? Term.List(Term.Tuple(
                Term.A(RuntimeExceptionNames.ErrorInfo),
                new MapTerm(new Dictionary<Term, Term>
                {
                    [Term.A(RuntimeExceptionNames.Module)] = Term.A(formatter)
                })
            ))
            : Nil.Value);
        var trace = new List<Term>
        {
            Term.Tuple(
                Term.A(module),
                Term.A(function),
                Cons.From(arguments),
                location
            )
        };
        while (frame is not null && trace.Count < depth)
        {
            trace.Add(Term.Tuple(
                Term.A(frame.Module),
                Term.A(frame.Function),
                frame.HostHandlerArguments is { } handlerArguments ? Cons.From(handlerArguments) : Term.I(frame.Arity),
                sourceLocation is not null ? frame.SourceLocation ?? Nil.Value : Nil.Value
            ));
            frame = frame.Parent;
        }

        return error.WithCapturedStack(Cons.From(trace));
    }

    internal static ValueTask<Term> InvokeNative(
        ProcessContext context,
        ErlangFunction body,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        ErlangExecutionFrame? frame = context.CaptureExecutionChain();
        Term[] savedArguments = arguments.ToArray();
        try
        {
            ValueTask<Term> pending = body(context, arguments);

            return pending.IsCompletedSuccessfully ? pending
                : AwaitNative(
                    pending,
                    context,
                    frame,
                    module,
                    function,
                    savedArguments
                );
        }
        catch (ErlangException error)
        {
            throw Call(
                context,
                error,
                frame,
                module,
                function,
                savedArguments,
                true
            );
        }
    }

    private static async ValueTask<Term> AwaitNative(
        ValueTask<Term> pending,
        ProcessContext context,
        ErlangExecutionFrame? frame,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        try
        {
            return await pending;
        }
        catch (ErlangException error)
        {
            throw Call(
                context,
                error,
                frame,
                module,
                function,
                arguments,
                true
            );
        }
    }

    internal static async ValueTask<Term> InvokeWrapper(
        ProcessContext context,
        ErlangFunction body,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        using var scope = context.EnterSourceWrapper(module, function, arguments.Count);
        ErlangExecutionFrame? frame = context.CaptureExecutionChain();
        Term[] savedArguments = arguments.ToArray();
        try
        {
            return await body(context, arguments);
        }
        catch (ErlangException error)
        {
            throw Source(
                context,
                error,
                frame,
                error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)) ? savedArguments : null
            );
        }
    }

    internal static async ValueTask<Term> InvokeHandler(
        ProcessContext context,
        ErlangFunction body,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        bool hostHandler
    )
    {
        Term[] savedArguments = arguments.ToArray();
        using var selection = context.EnterSelectedErrorHandler(module, function, savedArguments);
        using var scope = hostHandler ? context.EnterHostErrorHandler(module, function, savedArguments) : null;
        ErlangExecutionFrame? frame = context.CaptureExecutionChain();
        try
        {
            return await body(context, arguments);
        }
        catch (ErlangException error)
        {
            if (!hostHandler)
                throw;
            throw Source(
                context,
                error,
                frame,
                savedArguments,
                true
            );
        }
    }
}
