namespace Erlang.Tests;

internal static class ProcessInfoLogicalHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var worker = runtime.Spawn(async context =>
        {
            for (int index = 0; index < ProcessInfoLogicalHostNames.ChargedCalls; index++)
                await context.ReduceAsync();
            Term count = await Query(context, context.Self, ProcessInfoLogicalNames.Reductions);
            Require(count is TupleTerm { Items: [Atom, Integer value] } && value.Value >= ProcessInfoLogicalHostNames.ChargedCalls);
            Term defaults = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessInfoNames.Function,
                context.Self,
                Term.List(Term.A(ProcessInfoLogicalNames.Priority), Term.A(ProcessInfoLogicalNames.ErrorHandler))
            );
            Require(defaults.Equals(Term.List(
                Term.Tuple(Term.A(ProcessInfoLogicalNames.Priority), Term.A(ProcessInfoLogicalNames.NormalPriority)),
                Term.Tuple(Term.A(ProcessInfoLogicalNames.ErrorHandler), Term.A(ProcessInfoLogicalNames.DefaultErrorHandler))
            )));
            foreach (string key in new[] { ProcessInfoLogicalNames.InitialCall, ProcessInfoLogicalNames.Reductions, ProcessInfoLogicalNames.Priority, ProcessInfoLogicalNames.ErrorHandler, ProcessInfoLogicalNames.Suspending })
            {
                try
                {
                    await Query(context, Term.I(0), key);
                    throw new InvalidOperationException(ProcessInfoLogicalHostNames.Failure);
                }
                catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                }
            }

            return Nil.Value;
        });
        Require((await worker.Completion).Equals(Term.A(ProcessExitReasons.Normal)));
        var observer = runtime.Spawn(async context =>
        {
            foreach (string key in new[] { ProcessInfoLogicalNames.InitialCall, ProcessInfoLogicalNames.Reductions, ProcessInfoLogicalNames.Priority, ProcessInfoLogicalNames.ErrorHandler, ProcessInfoLogicalNames.Suspending })
                Require((await Query(context, worker.Pid, key)).Equals(Term.A(ProcessRegistryAtoms.Undefined)));

            return Nil.Value;
        });
        Require((await observer.Completion).Equals(Term.A(ProcessExitReasons.Normal)));
    }

    private static ValueTask<Term> Query(ProcessContext context, Term pid, string key) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        ProcessInfoNames.Function,
        pid,
        Term.A(key)
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessInfoLogicalHostNames.Failure);
    }
}
