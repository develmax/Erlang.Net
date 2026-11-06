namespace Erlang.Otp;

internal static class ErpcSource
{
    internal static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(ErpcSourceLines.File)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
    );

    internal static ErlangException Capture(
        ProcessContext context,
        ErlangException error,
        string function,
        int arity,
        int line
    )
    {
        using var frame = context.EnterGeneratedFunction(
            ErpcNames.Module,
            function,
            arity,
            Location(line),
            new object()
        );

        return new ErlangException(error.Reason, error.ExceptionClass).WithCapturedStack(context.CaptureErrorStack(Term.I(arity), null));
    }
}
