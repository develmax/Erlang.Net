namespace Erlang.HaltProbe;

public sealed record HaltProbeEvidence(
    string Stage,
    string Backend,
    RuntimeHaltKind? RequestKind,
    int? LogicalStatus,
    string? Slogan,
    string? EntryExitReason,
    string? Error,
    string? DumpFormat,
    IReadOnlyList<string> SourceSha256
)
{
    public string? LogicalStack { get; init; }
}
