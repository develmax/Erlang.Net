namespace Erlang.Otp;

public sealed record ServerInitialization(
    Term? State,
    Term? Error = null,
    bool Ignore = false,
    TimeSpan? Timeout = null,
    Term? Continue = null,
    Term? StartupExitReason = null
);
