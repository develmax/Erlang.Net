namespace Erlang;

internal static class RuntimeNodeKernelRequest
{
    public static async ValueTask<Term> Call(ProcessContext context, Term request)
    {
        while (true)
        {
            if (context.Runtime.WhereIs(RuntimeNodeControlNames.Kernel) is not Pid)
                return Term.A(RuntimeNodeControlNames.Ignored);
            try
            {
                return await context.Runtime.Modules.Call(
                    context,
                    RuntimeNodeControlNames.Server,
                    RuntimeNodeControlNames.Call,
                    Term.A(RuntimeNodeControlNames.Kernel),
                    request,
                    Term.A(RuntimeNodeControlNames.Infinity)
                );
            }
            catch (ErlangException exception) when (exception.ExceptionClass == ErlangExceptionClasses.Exit &&
                exception.Reason is TupleTerm { Items.Count: 2 } failure &&
                failure.Items[0] is Atom { Name: ErlangErrorReasons.NoProcess or ProcessExitReasons.Shutdown or ProcessExitReasons.Killed })
            {
                await context.ReduceAsync();
            }
        }
    }
}
