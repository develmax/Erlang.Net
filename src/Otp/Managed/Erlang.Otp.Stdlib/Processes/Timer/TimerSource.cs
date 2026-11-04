namespace Erlang;

internal static class TimerSource
{
    internal static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(TimerSourceFile.Name)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
    );

    internal static async ValueTask<Term> Run(
        ProcessContext context,
        string function,
        int arity,
        int line,
        Func<ValueTask<Term>> body
    )
    {
        using var frame = context.EnterGeneratedFunction(
            TimerLibrary.Name,
            function,
            arity,
            Location(line),
            new object()
        );
        try
        {
            return await body();
        }
        catch (ErlangException error)
        {
            throw error.WithCapturedStack(context.CaptureErrorStack(Term.I(arity), null));
        }
    }

    internal static ErlangException Clause(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        int line
    )
    {
        using var frame = context.EnterGeneratedFunction(
            TimerLibrary.Name,
            function,
            arguments.Count,
            Location(line),
            new object()
        );

        return new ErlangException(ErlangErrorReasons.FunctionClause)
            .WithCapturedStack(context.CaptureErrorStack(Cons.From(arguments), null));
    }

    internal static ErlangException CallFailure(ProcessContext context, Term reason)
        => ErlangFunctionCallErrors.Capture(context, reason);

    internal static ErlangException Error(
        ProcessContext context,
        string function,
        int arity,
        int line,
        Term reason
    )
    {
        using var frame = context.EnterGeneratedFunction(
            TimerLibrary.Name,
            function,
            arity,
            Location(line),
            new object()
        );

        return new ErlangException(reason).WithCapturedStack(context.CaptureErrorStack(Term.I(arity), null));
    }
}
