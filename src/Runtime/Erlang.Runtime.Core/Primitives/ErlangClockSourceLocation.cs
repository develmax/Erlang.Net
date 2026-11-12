namespace Erlang;

internal static class ErlangClockSourceLocation
{
    internal const string File = "erlang.erl";
    internal const int ConvertLine = 13495;
    internal const string ErrorModule = RuntimeNativeErrorMetadata545Names.ErrorFormatter;
    internal static readonly Term Convert = Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(File)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(ConvertLine))
    );
    internal static readonly MapTerm ErrorInfo = new([
        new KeyValuePair<Term, Term>(Term.A(RuntimeNativeErrorMetadata545Names.Module), Term.A(ErrorModule))]);
}
