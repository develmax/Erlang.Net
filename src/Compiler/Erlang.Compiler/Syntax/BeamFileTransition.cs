namespace Erlang.Compiler;

internal sealed record BeamFileTransition(
    int TokenIndex,
    string? File,
    int Line,
    Token? Directive = null,
    int Column = 1,
    string? IncludeRequest = null,
    string? RecordedIncludePath = null,
    BeamFileTransition.MappedParent? ParentMapping = null
)
{
    internal sealed record MappedParent(string File, int LineOffset, string? UnformattedParent);
}
