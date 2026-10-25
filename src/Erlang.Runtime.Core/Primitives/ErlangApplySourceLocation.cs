namespace Erlang;

internal static class ErlangApplySourceLocation
{
    // The preloaded apply/2 error continuation in the pinned OTP-29.1.1 image.
    internal const string File = "erlang.erl";
    internal const int FunctionApplyLine = 10558;
    internal const int FunctionApplyArity = 2;
    internal static readonly Term Location = Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(File)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(FunctionApplyLine))
    );
}
