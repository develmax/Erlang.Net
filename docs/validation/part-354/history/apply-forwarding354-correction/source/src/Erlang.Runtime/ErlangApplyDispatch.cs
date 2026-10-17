namespace Erlang;

internal static class ErlangApplyDispatch
{
    internal static ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        string function = CoreModules.AtomName(arguments[ErlangApplyLayout.FunctionArgument]);
        string module = CoreModules.AtomName(arguments[ErlangApplyLayout.ModuleArgument]);
        var values = new List<Term>();
        Term remaining = arguments[ErlangApplyLayout.ArgumentList];
        while (remaining is Cons cell)
        {
            if (values.Count == ErlangApplyLimits.MaximumArgumentCount)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            values.Add(cell.Head);
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return InvokeTarget(
            context,
            module,
            function,
            values.ToArray()
        );
    }

    private static async ValueTask<Term> InvokeTarget(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments
    )
    {
        ModuleCallDispatchKind dispatch = ModuleCallDispatchKind.Direct;
        try
        {
            return await context.Runtime.Modules.CallWithDispatch(
                context,
                module,
                function,
                arguments,
                out dispatch
            );
        }
        catch (ErlangException exception) when (dispatch == ModuleCallDispatchKind.Direct && !exception.ExplicitStackTrace && exception.StackTraceTerm is Nil)
        {
            throw RuntimeExceptionCapture.Call(
                context,
                exception,
                context.CaptureExecutionChain(),
                module,
                function,
                arguments,
                false
            );
        }
    }
}
