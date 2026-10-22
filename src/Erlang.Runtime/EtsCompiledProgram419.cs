namespace Erlang;

// The AST is the actual managed executable program representation, not an
// accounting shadow or a marker claiming an absent native allocation.
internal sealed class EtsCompiledProgram419(MatchSpecProgram program)
{
    internal MatchSpecProgram Program { get; } = program;
}
