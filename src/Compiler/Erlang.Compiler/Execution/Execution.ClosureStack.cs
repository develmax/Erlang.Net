namespace Erlang.Compiler;

public static partial class Execution
{
    private static int AnonymousStackArity(
        Expr.Fun expression,
        IReadOnlyDictionary<string, Term> environment,
        int callableArity
    )
    {
        // OTP names the lifted implementation in stack frames: its formal
        // parameters plus lexical free variables. Callable arity stays unchanged.
        int freeCount = ClosureCaptures.Names(expression)?.Length
            ?? ClosureCaptures.Capture(expression, environment).Count;

        return checked(callableArity + freeCount);
    }
}
