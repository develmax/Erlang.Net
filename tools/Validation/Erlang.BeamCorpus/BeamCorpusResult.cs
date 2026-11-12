internal sealed record BeamCorpusResult(
    string Beam,
    string Status,
    string? Source = null,
    string? Detail = null,
    int? FirstDifferentByte = null,
    [property: System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    string? EmissionSite = null
);
