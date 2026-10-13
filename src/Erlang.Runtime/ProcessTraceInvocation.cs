namespace Erlang;

internal sealed record ProcessTraceInvocation(Term Mfa, bool ReturnTrace, bool ExceptionTrace);
