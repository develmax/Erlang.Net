namespace Erlang.HaltProbe;

internal sealed record HaltRawProcessEvidence(
    int ProcessId,
    int? RawExitCode,
    bool TimedOut,
    bool StandardOutputComplete,
    bool StandardErrorComplete,
    string? InfrastructureError
);
