namespace Erlang;

internal static class BeamTryExecution
{
    internal static ErlangException Raise(BeamSavedTrace trace, Term reason, string? exceptionClass = null)
        => new(reason, exceptionClass ?? trace.ExceptionClass)
        {
            StackTraceTerm = trace.StackTraceTerm,
            ExplicitStackTrace = trace.ExplicitStackTrace,
            AutomaticStackTraceCaptured = trace.AutomaticStackTraceCaptured
        };
}
