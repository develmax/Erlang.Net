namespace Erlang.Compiler;

internal sealed class SourceTailCall(
    ModuleDefinition module,
    string function,
    IReadOnlyList<Term> arguments
) : Exception
{
    public ModuleDefinition Module { get; } = module;
    public string Function { get; } = function;
    public IReadOnlyList<Term> Arguments { get; } = arguments;
}
