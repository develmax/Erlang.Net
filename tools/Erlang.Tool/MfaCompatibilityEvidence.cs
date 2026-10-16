using System.Text.Json.Serialization;

internal sealed record MfaCompatibilityEvidence
{
    [JsonRequired] public required string Kind { get; init; }
    [JsonRequired] public required string Path { get; init; }
    [JsonRequired] public required string Sha256 { get; init; }
    [JsonRequired] public required string Scope { get; init; }
}
