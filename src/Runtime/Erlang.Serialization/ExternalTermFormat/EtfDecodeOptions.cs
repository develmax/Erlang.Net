namespace Erlang;

public sealed record EtfDecodeOptions
{
    internal AtomNameTable? AtomTable { get; init; }
    internal Func<IReadOnlyList<EtfNodeEpoch>, EtfDecodeAdmission>? NodeAdmission { get; init; }
    internal LocalNodeIdentity? NodeIdentity { get; init; }
    internal bool DistributionDebug423 { get; init; }
    internal Atom[]? AtomTranslationCache423 { get; init; }
    public bool Safe { get; init; }
    public string? LocalNode { get; init; }
    public uint LocalCreation { get; init; }
    public uint? LocalSeed { get; init; }
    public Func<ExternalFunctionIdentity, bool, FunctionTerm>? ExternalFunctionFactory { get; init; }
    public Func<EtfLocalFunctionCode, int, IReadOnlyList<Term>, FunctionTerm>? LocalFunctionFactory { get; init; }
    public Func<Pid, Pid>? PidFactory { get; init; }
    public Func<PortTerm, PortTerm>? PortFactory { get; init; }
    public Func<ReferenceTerm, ReferenceTerm>? ReferenceFactory { get; init; }
}
