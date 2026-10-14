namespace Erlang;

public sealed record EtfDecodeOptions
{
    public bool Safe { get; init; }
    public string? LocalNode { get; init; }
    public uint LocalCreation { get; init; }
    public uint? LocalSeed { get; init; }
    public Func<ExternalFunctionIdentity, bool, FunctionTerm>? ExternalFunctionFactory { get; init; }
    public Func<EtfLocalFunctionCode, int, IReadOnlyList<Term>, FunctionTerm>? LocalFunctionFactory { get; init; }
    public Func<ReferenceTerm, ReferenceTerm>? ReferenceFactory { get; init; }
}
