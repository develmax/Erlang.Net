namespace Erlang.Tests;

public static class ProcessSavedCallHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime(ProcessNodeDefaults.Local, null, ProcessSavedCallHostParameters.Capacity);
        await runtime.InitializeModulesAsync(registry =>
        {
            foreach (string function in new[] { ProcessSavedCallHostNames.Alpha, ProcessSavedCallHostNames.Beta, ProcessSavedCallHostNames.Gamma })
                registry.Register(
                    ProcessSavedCallHostNames.Module,
                    function,
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Term.I(0))
                );
        });
        Exception? failure = null;
        var owner = runtime.Spawn(async context =>
        {
            try
            {
                Term old = await Flag(
                    runtime,
                    context,
                    context.Self,
                    ProcessSavedCallHostParameters.RingCapacity
                );
                Require(old.Equals(Term.I(0)));
                foreach (string function in new[] { ProcessSavedCallHostNames.Alpha, ProcessSavedCallHostNames.Beta, ProcessSavedCallHostNames.Gamma })
                    await runtime.Modules.Call(context, ProcessSavedCallHostNames.Module, function);
                Term? observed = null;
                var observer = runtime.Spawn(async other =>
                {
                    observed = await runtime.Modules.Call(
                        other,
                        ErlangModuleNames.Module,
                        ProcessInfoNames.Function,
                        context.Self,
                        Term.A(ProcessInfoNames.LastCalls)
                    );

                    return Term.I(0);
                });
                await observer.Completion.WaitAsync(ProcessSavedCallHostParameters.Timeout);
                var info = (TupleTerm)observed!;
                Require(info.Items[1].Equals(Term.List(
                    Term.Tuple(Term.A(ProcessSavedCallHostNames.Module), Term.A(ProcessSavedCallHostNames.Beta), Term.I(0)),
                    Term.Tuple(Term.A(ProcessSavedCallHostNames.Module), Term.A(ProcessSavedCallHostNames.Gamma), Term.I(0))
                )));
                Require((await Flag(
                    runtime,
                    context,
                    context.Self,
                    0
                )).Equals(Term.I(ProcessSavedCallHostParameters.RingCapacity)));
                for (int round = 0; round < ProcessSavedCallHostParameters.DeathRounds; round++)
                {
                    var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var target = runtime.Spawn(async child =>
                    {
                        entered.SetResult();
                        await blocked.Task.WaitAsync(child.Cancellation);

                        return Term.I(0);
                    });
                    await entered.Task.WaitAsync(ProcessSavedCallHostParameters.Timeout);
                    ValueTask<Term> request = Flag(
                        runtime,
                        context,
                        target.Pid,
                        ProcessSavedCallHostParameters.RingCapacity
                    );
                    Require(!request.IsCompleted);
                    runtime.Exit(context, target.Pid, Term.A(ProcessExitSignals.Kill));
                    bool rejected = false;
                    try
                    {
                        await request.AsTask().WaitAsync(ProcessSavedCallHostParameters.Timeout);
                    }
                    catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                    {
                        rejected = true;
                    }
                    Require(rejected);
                    await target.Completion.WaitAsync(ProcessSavedCallHostParameters.Timeout);
                    Require(!runtime.IsAlive(target.Pid));
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Term.I(0);
        });
        await owner.Completion.WaitAsync(ProcessSavedCallHostParameters.Timeout);
        if (failure is not null)
            throw failure;
    }

    private static ValueTask<Term> Flag(
        ProcessRuntime runtime,
        ProcessContext context,
        Pid pid,
        int capacity
    ) => runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        ErlangModuleNames.ProcessFlag,
        pid,
        Term.A(ProcessSavedCallNames.Flag),
        Term.I(capacity)
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessSavedCallHostNames.Failure);
    }
}
