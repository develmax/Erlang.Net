namespace Erlang;

internal sealed class BeamSavedTrace
{
    internal BeamSavedTrace(ErlangException captured)
    {
        if (!captured.ExplicitStackTrace && !captured.AutomaticStackTraceCaptured)
            throw new InvalidDataException(BeamRegisterProtocolNames.Trace);
        ExceptionClass = captured.ExceptionClass;
        Reason = captured.Reason;
        StackTraceTerm = captured.StackTraceTerm;
        ExplicitStackTrace = captured.ExplicitStackTrace;
        AutomaticStackTraceCaptured = captured.AutomaticStackTraceCaptured;
    }

    internal string ExceptionClass { get; }
    internal Term Reason { get; }
    internal Term StackTraceTerm { get; }
    internal bool ExplicitStackTrace { get; }
    internal bool AutomaticStackTraceCaptured { get; }
}
