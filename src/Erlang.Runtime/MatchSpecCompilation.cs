namespace Erlang;

public sealed record MatchSpecCompilation(MatchSpecProgram? Program, IReadOnlyList<string> Diagnostics);
