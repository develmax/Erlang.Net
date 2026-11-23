namespace Erlang.Compiler;

public sealed record BeamIncludePathMap(
    string SourceArgument,
    BeamIncludeSearchDirectory WorkingDirectory,
    IReadOnlyList<BeamIncludeSearchDirectory> IncludeDirectories,
    IReadOnlyDictionary<string, BeamIncludeSearchDirectory> Libraries
)
{
    /// <summary>Overrides the main file's physical directory for relative include lookup.</summary>
    public string? PhysicalSourceDirectory { get; init; }
}
