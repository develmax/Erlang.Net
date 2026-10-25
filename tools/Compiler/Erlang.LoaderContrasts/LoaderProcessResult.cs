namespace LoaderContrasts;

internal sealed record LoaderProcessResult(
    int ExitCode,
    bool TimedOut,
    bool RawComplete
);
