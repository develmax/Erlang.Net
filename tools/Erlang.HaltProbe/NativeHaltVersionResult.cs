namespace Erlang.HaltProbe;

internal sealed record NativeHaltVersionResult(
    int? RawExitCode,
    IReadOnlyList<byte> StandardOutput,
    IReadOnlyList<byte> StandardError,
    string? Version,
    bool TimedOut,
    string? InfrastructureError
);
