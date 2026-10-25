namespace Erlang.Compiler;

internal sealed class RuntimeTailCall(
    string module,
    string function,
    Term[] arguments
) : Exception
{
    public string Module { get; } = module;
    public string Function { get; } = function;
    public Term[] Arguments { get; } = arguments;
}
