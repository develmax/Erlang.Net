namespace Erlang.Compiler;

public static partial class Execution
{
    private sealed class FunctionTailCall(FunctionTerm function, IReadOnlyList<Term> arguments) : Exception
    {
        internal FunctionTerm Function { get; } = function;
        internal IReadOnlyList<Term> Arguments { get; } = arguments;
    }
}
