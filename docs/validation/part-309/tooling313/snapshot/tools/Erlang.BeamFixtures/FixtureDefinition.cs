namespace BeamFixtureExport;

internal sealed record FixtureDefinition(
    string Module,
    string Source,
    string Purpose,
    string? IndependentExpectedExpression
);
