namespace Erlang.HaltProbe;

public sealed record HaltSubprocessResult(
    string Name,
    HaltProcessBackend Backend,
    string Stage,
    int? RawExitCode,
    IReadOnlyList<byte> StandardOutput,
    IReadOnlyList<byte> StandardError,
    bool TimedOut,
    HaltProbeEvidence? Evidence,
    string? InfrastructureError,
    string WorkingDirectory,
    IReadOnlyList<HaltSourceUnit> Sources
);
