namespace Erlang.Tests;

internal sealed record TermOrderChildReceipt(
    string Family,
    string Operation,
    int Depth,
    int? ProcessId,
    double ElapsedSeconds,
    int? ExitCode,
    bool Started,
    bool TimedOut,
    bool KillRequested,
    bool Settled,
    bool StandardOutputComplete,
    bool StandardErrorComplete,
    string[] LifecycleFailures,
    string TermsDllSha256,
    string DriverPath,
    string StandardOutputSha256,
    string StandardErrorSha256
);
