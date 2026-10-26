namespace NodeIdentityContrasts;

internal sealed record NodeProcessResult(
    int ExitCode,
    bool TimedOut,
    bool RawComplete
);
