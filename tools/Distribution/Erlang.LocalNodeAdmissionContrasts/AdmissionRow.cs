namespace AdmissionOracle;

internal sealed record AdmissionRow(
    string Scenario,
    int ExitCode,
    bool TimedOut,
    bool RawComplete,
    string? Stage,
    bool Captured
);
