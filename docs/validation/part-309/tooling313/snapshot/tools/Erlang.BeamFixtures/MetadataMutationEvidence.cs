namespace BeamFixtureExport;

internal sealed record MetadataMutationEvidence(
    string Name,
    string Chunk,
    bool ExpectedLoadAccepted,
    string IndependentExpectedExpression,
    string BeamSha256,
    string PayloadSha256,
    string ActualEtfSha256,
    string ExpectedEtfSha256,
    string EvaluationSha256,
    int ExitCode
);
