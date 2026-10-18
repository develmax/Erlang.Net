namespace Erlang.Compiler;

public static partial class Execution
{
    private static async ValueTask<Term> EvaluateGeneratedTail(
        Expr expression,
        ProcessContext context,
        Dictionary<string, Term> bindings,
        ModuleDefinition? module
    )
    {
        try
        {
            return await Evaluate(
                expression,
                context,
                bindings,
                module,
                true
            );
        }
        catch (TailCall next) when (module is not null)
        {
            return await InvokeAsync(
                module,
                next.Function,
                context,
                next.Arguments,
                next.Image
            );
        }
    }
}
