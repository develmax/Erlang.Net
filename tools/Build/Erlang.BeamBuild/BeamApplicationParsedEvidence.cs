namespace Erlang.BeamBuild;

internal sealed record BeamApplicationParsedEvidence(
    IReadOnlyList<string> ModuleOrder,
    IReadOnlyList<string> PropertyOrder,
    IReadOnlyList<string> OrderedConfigNames,
    int ResultBytes,
    string ResultSha256
);
