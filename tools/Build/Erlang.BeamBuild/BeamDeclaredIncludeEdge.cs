namespace Erlang.BeamBuild;

internal sealed record BeamDeclaredIncludeEdge(
    string Includer,
    int Offset,
    string Literal,
    string[] Candidates,
    string Selected
);
