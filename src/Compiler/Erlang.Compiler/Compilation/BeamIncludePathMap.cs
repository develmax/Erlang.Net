namespace Erlang.Compiler;

public sealed record BeamIncludePathMap(
    string SourceArgument,
    BeamIncludeSearchDirectory WorkingDirectory,
    IReadOnlyList<BeamIncludeSearchDirectory> IncludeDirectories,
    IReadOnlyDictionary<string, BeamIncludeSearchDirectory> Libraries
);
