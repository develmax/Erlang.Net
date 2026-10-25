namespace ApplyOptimizerContrasts;

internal sealed record ApplyProcessResult(
    int ExitCode,
    bool TimedOut,
    bool RawComplete
);
