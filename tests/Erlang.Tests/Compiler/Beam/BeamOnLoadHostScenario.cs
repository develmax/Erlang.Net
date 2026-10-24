namespace Erlang.Tests;

internal sealed record BeamOnLoadHostScenario(
    string Name,
    string Module,
    bool Old,
    bool Success
);
