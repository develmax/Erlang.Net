namespace BeamFixtureExport;

internal sealed record FixtureMetadataEvidence(
    int ExitCode,
    bool FileOnlyQuery,
    bool ModuleLoadPerformed,
    string EtfFile,
    string EtfSha256,
    string TextFile,
    string TextSha256,
    string EvaluationFile,
    string EvaluationSha256,
    string StandardOutputSha256,
    string StandardErrorSha256
);
