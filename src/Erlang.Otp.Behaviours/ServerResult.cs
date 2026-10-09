namespace Erlang.Otp;

public sealed record ServerResult(Term State, Term? Reply = null, Term? StopReason = null);
