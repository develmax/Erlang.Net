namespace BeamFixtureExport;

internal sealed record OnLoadObserverScenario(
    string Name,
    string Module,
    bool Succeeds,
    bool HasOldImage
);
