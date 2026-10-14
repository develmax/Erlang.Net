namespace NodeIdentityContrasts;

internal sealed record NodeObservation(
    string Name,
    int ExitCode,
    bool TimedOut,
    bool RawComplete,
    string? Stage,
    bool Captured
);
