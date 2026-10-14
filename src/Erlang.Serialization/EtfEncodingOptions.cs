namespace Erlang;

public sealed record EtfEncodingOptions
{
    internal LocalNodeIdentity? NodeIdentity { get; init; }
    internal LocalNodeSnapshot? NodeSnapshot { get; init; }
    public int MinorVersion { get; init; } = EtfEncodingParameters.DefaultMinorVersion;
    public int CompressionLevel { get; init; }
    public bool Deterministic { get; init; }
    public bool Local { get; init; }
    public uint LocalSeed { get; init; }
    public string? LocalNode { get; init; }
    public uint LocalCreation { get; init; }
    public Pid? FunctionOwner { get; init; }
}
