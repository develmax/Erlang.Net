namespace Erlang.HaltProbe;

public sealed record HaltSubprocessContract(
    string Name,
    IReadOnlyList<HaltSourceUnit> Sources,
    string Module,
    string Entry,
    HaltProcessObservation ExpectedObservation,
    RuntimeHaltKind? ExpectedRequestKind,
    int? ExpectedLogicalStatus,
    int? ExpectedPosixExit,
    int? ExpectedWindowsExit,
    IReadOnlyList<byte> ExpectedStandardOutput,
    IReadOnlyList<byte>? ExpectedPosixStandardError,
    IReadOnlyList<byte>? ExpectedWindowsStandardError,
    IReadOnlyList<string> ForbiddenMarkers,
    bool ComparableToOriginal,
    string? PhysicalFormatGap = null
);
