namespace BeamFixtureExport;

internal sealed record FixtureExecutionEvidence(
    bool Executed,
    string? NotExecutedReason,
    string? IndependentExpectedExpression,
    int? ExitCode,
    string? ActualEtfFile,
    string? ActualEtfSha256,
    string? ExpectedEtfFile,
    string? ExpectedEtfSha256,
    string? EvaluationFile,
    string? EvaluationSha256,
    string? StandardOutputSha256,
    string? StandardErrorSha256
);
