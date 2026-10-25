using System.Text.Json.Serialization;

internal sealed record MfaCompatibilitySourceFile
{
    [JsonRequired] public required string Path { get; init; }
    [JsonRequired] public required string Sha256 { get; init; }
}
