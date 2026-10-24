namespace Erlang.Tests;

internal static class SpawnPriorityRetirementHostChecks
{
    internal static async Task HaltSettlesEscapedCallbackWithoutInvokingTarget()
    {
        await using var runtime = new ProcessRuntime(1);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int targets = 0;
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.Target,
            0,
            (_, _) =>
            {
                Interlocked.Increment(ref targets);

                return ValueTask.FromResult<Term>(Term.A(SpawnPriorityLifecycleHostNames.Done));
            }
        );
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.Bridge,
            0,
            async (context, _) =>
            {
                try
                {
                    entered.TrySetResult();
                    await release.Task.ConfigureAwait(false);

                    return await runtime.Modules.Call(context, SpawnPriorityLifecycleHostNames.Module,
                        SpawnPriorityLifecycleHostNames.Target);
                }
                finally
                {
                    unwound.TrySetResult();
                }
            }
        );
        var parent = runtime.Spawn(context => runtime.Modules.Call(context,
            SpawnPriorityLifecycleHostNames.Module, SpawnPriorityLifecycleHostNames.Bridge));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
            _ = runtime.RequestImmediateHalt(SpawnPriorityLifecycleHostNames.SuccessfulHaltStatus);
        }
        finally
        {
            release.TrySetResult();
        }
        await runtime.HaltQuiescence.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        await unwound.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        if (!parent.Completion.IsCompleted || Volatile.Read(ref targets) != 0)
            throw new InvalidOperationException(SpawnPriorityLifecycleHostNames.Failure);
    }

    internal static async Task KilledOwnerStillSettlesQueuedCleanup()
    {
        await using var runtime = new ProcessRuntime(1);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int targets = 0;
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.Target,
            0,
            (_, _) =>
            {
                Interlocked.Increment(ref targets);

                return ValueTask.FromResult<Term>(Term.A(SpawnPriorityLifecycleHostNames.Done));
            }
        );
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.Bridge,
            0,
            async (context, _) =>
            {
                try
                {
                    entered.TrySetResult(context);
                    await release.Task.ConfigureAwait(false);

                    return await runtime.Modules.Call(context, SpawnPriorityLifecycleHostNames.Module,
                        SpawnPriorityLifecycleHostNames.Target);
                }
                finally
                {
                    unwound.TrySetResult();
                }
            }
        );
        var parent = runtime.Spawn(context => runtime.Modules.Call(context,
            SpawnPriorityLifecycleHostNames.Module, SpawnPriorityLifecycleHostNames.Bridge));
        var owner = await entered.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        try
        {
            runtime.Exit(owner, parent.Pid, Term.A(SpawnPriorityLifecycleHostNames.Kill));
        }
        finally
        {
            release.TrySetResult();
        }
        await unwound.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        await runtime.DisposeAsync();
        var field = typeof(ProcessRuntime).GetField(
            SpawnPriorityLifecycleHostNames.DispatcherField,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        )!;
        object dispatcher = field.GetValue(runtime)!;
        var completion = (Task)dispatcher.GetType().GetProperty(SpawnPriorityLifecycleHostNames.CompletionProperty)!.GetValue(dispatcher)!;
        await completion.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        if (!parent.Completion.IsCompleted || Volatile.Read(ref targets) != 0)
            throw new InvalidOperationException(SpawnPriorityLifecycleHostNames.Failure);
    }
}
