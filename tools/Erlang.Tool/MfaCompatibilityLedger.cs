using System.Text.Json.Serialization;

internal sealed record MfaCompatibilityLedger
{
    [JsonRequired] public required int SchemaVersion { get; init; }
    [JsonRequired] public required string Tag { get; init; }
    [JsonRequired] public required string Commit { get; init; }
    [JsonRequired] public required MfaCompatibilitySourceFile[] VerifiedSourceFiles { get; init; }
    [JsonRequired] public required MfaCompatibilityEntry[] Entries { get; init; }
}
