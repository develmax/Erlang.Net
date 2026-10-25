namespace CreationBoundaryContrasts;

internal sealed record CreationRow(
    string Scenario,
    int? ExitCode,
    bool? TimedOut,
    bool? RawComplete,
    string? Stage,
    bool Captured,
    string? CollectorException
);
