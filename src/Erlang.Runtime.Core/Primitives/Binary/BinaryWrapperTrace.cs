namespace Erlang;

internal static class BinaryWrapperTrace
{
    private static readonly object replaceContinuation = new();

    internal static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(BinaryWrapperNames.File), Term.String(BinaryWrapperNames.FileName)),
        Term.Tuple(Term.A(BinaryWrapperNames.Line), Term.I(line))
    );

    internal static ErlangException Public(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        bool badOption = false,
        int line = BinaryWrapperSource.BadArgumentInfo
    )
    {
        Term metadata = ErrorMetadata(line, badOption);
        var first = new ErlangExecutionFrame(
            BinaryModuleNames.Module,
            function,
            arguments.Count,
            null,
            SourceLocation: metadata
        );
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        if (caller is { SourceWrapper: true, Module: BinaryModuleNames.Module } && caller.Function == function)
            caller = caller.Parent;
        first = first with { Parent = caller };

        return RuntimeExceptionCapture.Source(
            context,
            new ErlangException(ErlangErrorReasons.BadArgument),
            first,
            arguments
        );
    }

    internal static Term ErrorMetadata(int line, bool badOption = false)
    {
        var info = new Dictionary<Term, Term> { [Term.A(BinaryWrapperNames.Module)] = Term.A(BinaryWrapperNames.Formatter) };
        if (badOption)
            info[Term.A(BinaryWrapperNames.Cause)] = Term.A(BinaryWrapperNames.BadOption);

        return Term.List(
            Term.Tuple(Term.A(BinaryWrapperNames.File), Term.String(BinaryWrapperNames.FileName)),
            Term.Tuple(Term.A(BinaryWrapperNames.Line), Term.I(badOption ? BinaryWrapperSource.BadArgumentCause : line)),
            Term.Tuple(Term.A(BinaryWrapperNames.ErrorInfo), new MapTerm(info))
        );
    }

    internal static async ValueTask<Term> Callback(
        ProcessContext context,
        FunctionTerm callback,
        Term matched,
        bool pending
    )
    {
        await context.ChargeAsync(0);
        using var history = pending ? context.EnterGeneratedFunction(
            BinaryModuleNames.Module,
            BinaryWrapperNames.DoReplace,
            4,
            Location(BinaryWrapperSource.ReplaceContinuation),
            replaceContinuation
        ) : null;
        using var current = context.EnterGeneratedFunction(
            BinaryModuleNames.Module,
            BinaryWrapperNames.DoReplace,
            4,
            Location(BinaryWrapperSource.ReplaceCallback),
            new object()
        );

        return await callback.Invoke(context, [matched]);
    }
}
