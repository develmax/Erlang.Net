namespace Erlang.Compiler;

public static partial class Execution
{
    private static async ValueTask<Term> CallSourceLoadNif(
        ProcessContext context,
        Term[] arguments
    )
    {
        try
        {
            return await context.Runtime.Modules.InvokeSourceLoadNif(context, arguments);
        }
        catch (ErlangException exception)
        {
            throw RuntimeCallFrames.Capture(
                context,
                exception,
                ErlangModuleNames.Module,
                SourceLoadNifNames.Function,
                arguments
            );
        }
    }
}
