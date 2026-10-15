namespace Erlang;

internal static class ErlangApplyDispatch
{
    internal static ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        string module = CoreModules.AtomName(arguments[ErlangApplyLayout.ModuleArgument]);
        string function = CoreModules.AtomName(arguments[ErlangApplyLayout.FunctionArgument]);
        Term[] values = Cons.Items(arguments[ErlangApplyLayout.ArgumentList]).ToArray();

        return InvokeTarget(
            context,
            module,
            function,
            values
        );
    }

    private static async ValueTask<Term> InvokeTarget(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments
    )
    {
        try
        {
            return await context.Runtime.Modules.Call(
                context,
                module,
                function,
                arguments
            );
        }
        catch (ErlangException exception) when (!exception.ExplicitStackTrace && exception.StackTraceTerm is Nil)
        {
            throw exception.WithStackFrame(module, function, arguments);
        }
    }
}
