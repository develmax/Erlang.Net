namespace Erlang.Compiler;

internal sealed record BeamModuleMetadata(
    IReadOnlyList<ModuleAttribute> Attributes,
    byte[]? Documentation,
    byte[]? CompileInfo = null,
    byte[]? DebugInfo = null
);
