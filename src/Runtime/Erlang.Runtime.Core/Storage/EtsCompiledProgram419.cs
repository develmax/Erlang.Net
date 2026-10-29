namespace Erlang;

// Public compile still constructs the complete existing AST representation.
// Native634 is an explicit internal diagnostic constructor, never a fallback.
internal sealed class EtsCompiledProgram419
{
    internal EtsCompiledProgram419(MatchSpecProgram program) => Executable = new(program);

    internal EtsCompiledProgram419(MatchSavedProgram634 program) => Executable = new(program);

    internal EtsCompiledProgram419(EtsCompiledExecutable638 executable) => Executable = executable;

    internal EtsCompiledExecutable638 Executable { get; }
}
