namespace Erlang.Compiler;

internal sealed record BeamFileTransition(
    int TokenIndex,
    string? File,
    int Line,
    Token? Directive = null,
    int Column = 1,
    string? IncludeRequest = null
);
