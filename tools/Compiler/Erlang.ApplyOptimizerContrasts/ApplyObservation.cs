namespace ApplyOptimizerContrasts;

internal sealed record ApplyObservation(
    string Name,
    ApplyProcessResult Process,
    string? Stage,
    bool Captured,
    IReadOnlyList<object> Files
);
