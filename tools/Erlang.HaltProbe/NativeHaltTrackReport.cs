namespace Erlang.HaltProbe;

internal sealed record NativeHaltTrackReport(
    string Baseline,
    string DeclaredSourceCommit,
    bool VersionVerified,
    NativeHaltVersionResult? VersionProcess,
    bool Complete,
    int Planned,
    int Executed,
    int ComparablePlanned,
    int ComparablePassed,
    int ComparableFailed,
    int PhysicalEndpointPlanned,
    int PhysicalEndpointPassed,
    int PhysicalEndpointFailed,
    bool PhysicalFormatComparisonComplete,
    string? InfrastructureError,
    HaltSubprocessContract? AbortedContract,
    IReadOnlyList<NativeHaltTrackRow> Results
);
