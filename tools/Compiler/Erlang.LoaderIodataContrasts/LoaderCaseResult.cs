namespace LoaderIodataContrasts;

internal sealed record LoaderCaseResult(
    string Name,
    string Scenario,
    int ExitCode,
    bool TimedOut,
    bool RawComplete,
    string? Stage,
    bool Passed,
    IReadOnlyList<object> Files
);
