namespace BeamFixtureExport;

internal sealed record MetadataMutationCase(
    string Name,
    string PayloadKind,
    string TermExpression,
    string IndependentExpectedExpression,
    bool ExpectedLoadAccepted
);
