namespace Erlang.Tests;

internal static class SpawnRequestHostChecks
{
    public static async Task Run()
    {
        await Abandonment();
        await OwnerDeath();
        await ResourceFailure();
    }

    private static async Task Abandonment()
    {
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        var owner = runtime.Spawn(async context =>
        {
            try
            {
                for (int index = 0; index < SpawnRequestHostParameters.RaceAttempts; index++)
                {
                    var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var observed = new TaskCompletionSource<Term?>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var callback = new FunctionTerm(
                        0,
                        async (execution, _) =>
                    {
                        var child = (ProcessContext)execution;
                        child.TrapExits = true;
                        ready.SetResult(child);
                        await release.Task.WaitAsync(child.Cancellation);
                        observed.SetResult(await child.ReceiveAsync<Term>(value => value, SpawnRequestHostParameters.Timeout));

                        return Nil.Value;
                    }
                    );
                    var reference = (ReferenceTerm)await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ProcessSpawnRequestNames.Function,
                        callback,
                        Cons.From([Term.A(ProcessSpawnOptionNames.Link), Term.A(ProcessSpawnOptionNames.Monitor)])
                    );
                    ProcessContext childContext = await ready.Task.WaitAsync(SpawnRequestHostParameters.Timeout);
                    Require((await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ErlangModuleNames.Demonitor,
                        reference,
                        Cons.From([Term.A(SpawnRequestHostParameters.MonitorInfoOption)])
                    )).Equals(Term.A(ErlangBooleanAtoms.False)));
                    var foreignResult = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var foreign = runtime.Spawn(async other =>
                    {
                        foreignResult.SetResult(await runtime.Modules.Call(
                            other,
                            ErlangModuleNames.Module,
                            ProcessSpawnRequestNames.AbandonFunction,
                            reference
                        ));

                        return Nil.Value;
                    });
                    Require((await foreignResult.Task.WaitAsync(SpawnRequestHostParameters.Timeout)).Equals(Term.A(ErlangBooleanAtoms.False)));
                    Require((await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ProcessSpawnRequestNames.AbandonFunction,
                        reference
                    )).Equals(Term.A(ErlangBooleanAtoms.True)));
                    Require((await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ProcessSpawnRequestNames.AbandonFunction,
                        reference
                    )).Equals(Term.A(ErlangBooleanAtoms.False)));
                    release.SetResult();
                    Require((await observed.Task.WaitAsync(SpawnRequestHostParameters.Timeout))!.Equals(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), context.Self, Term.A(ProcessSpawnRequestNames.Abandoned))));
                    await WaitForDeath(runtime, childContext.Self).WaitAsync(SpawnRequestHostParameters.Timeout);
                    Require(context.Mailbox.Count == 0);
                    Require(!runtime.IsAlive(childContext.Self));
                    await foreign.Completion.WaitAsync(SpawnRequestHostParameters.Timeout);
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Nil.Value;
        });
        await owner.Completion.WaitAsync(SpawnRequestHostParameters.Timeout);
        if (failure is not null)
            throw failure;
    }

    private static async Task OwnerDeath()
    {
        await using var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new TaskCompletionSource<Term?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = runtime.Spawn(async context =>
        {
            var callback = new FunctionTerm(
                0,
                async (execution, _) =>
            {
                var child = (ProcessContext)execution;
                child.TrapExits = true;
                ready.SetResult(child);
                await release.Task.WaitAsync(child.Cancellation);
                observed.SetResult(await child.ReceiveAsync<Term>(value => value, SpawnRequestHostParameters.Timeout));

                return Nil.Value;
            }
            );
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSpawnRequestNames.Function,
                callback,
                Cons.From([Term.A(ProcessSpawnOptionNames.Link), Term.A(ProcessSpawnOptionNames.Monitor)])
            );
            await ready.Task.WaitAsync(SpawnRequestHostParameters.Timeout);
            context.Exit(Term.A(SpawnRequestHostParameters.OwnerDeath));

            return Nil.Value;
        });
        await owner.Completion.WaitAsync(SpawnRequestHostParameters.Timeout);
        release.SetResult();
        Require((await observed.Task.WaitAsync(SpawnRequestHostParameters.Timeout))!.Equals(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), owner.Pid, Term.A(SpawnRequestHostParameters.OwnerDeath))));
    }

    private static async Task ResourceFailure()
    {
        await using var runtime = new ProcessRuntime(ProcessNodeDefaults.Local, null, SpawnRequestHostParameters.ResourceCapacity);
        Exception? failure = null;
        var owner = runtime.Spawn(async context =>
        {
            try
            {
                var callback = new FunctionTerm(0, (_, _) => throw new InvalidOperationException(SpawnRequestHostParameters.Failure));
                var reference = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessSpawnRequestNames.Function,
                    callback
                );
                Term? reply = await context.ReceiveAsync<Term>(value => value, SpawnRequestHostParameters.Timeout);
                Require(reply!.Equals(Term.Tuple(
                    Term.A(ProcessSpawnRequestNames.DefaultTag),
                    reference,
                    Term.A(ProcessSpawnRequestNames.Error),
                    Term.A(ErlangErrorReasons.SystemLimit)
                )));
                Require((await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessSpawnRequestNames.AbandonFunction,
                    reference
                )).Equals(Term.A(ErlangBooleanAtoms.False)));
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Nil.Value;
        });
        await owner.Completion.WaitAsync(SpawnRequestHostParameters.Timeout);
        if (failure is not null)
            throw failure;
    }

    private static async Task WaitForDeath(ProcessRuntime runtime, Pid child)
    {
        while (runtime.IsAlive(child))
            await Task.Yield();
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SpawnRequestHostParameters.Failure);
    }
}
