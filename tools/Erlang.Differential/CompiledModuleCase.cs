namespace Erlang.Differential;

public sealed record CompiledModuleCase(
    string Name,
    string Source,
    Term Expected,
    CompiledModuleCallerContext CallerContext = CompiledModuleCallerContext.UntrappedFresh,
    CompiledModuleCompilerPolicy CompilerPolicy = CompiledModuleCompilerPolicy.Default
)
{
    public string? ExpectedStandardOutputHex { get; init; }
    public string? ExpectedStandardErrorHex { get; init; }
}
