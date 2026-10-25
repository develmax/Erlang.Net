using System.Text.Json.Serialization;

internal sealed record MfaCompatibilityEntry
{
    [JsonRequired] public required string Module { get; init; }
    [JsonRequired] public required string Function { get; init; }
    [JsonRequired] public required int Arity { get; init; }
    [JsonRequired] public required string Status { get; init; }
    [JsonRequired] public required string Scope { get; init; }
    [JsonRequired] public required MfaCompatibilityEvidence[] Evidence { get; init; }
    [JsonRequired] public required string[] Limits { get; init; }

    public string Key => Module + ":" + Function + "/" + Arity;
}
