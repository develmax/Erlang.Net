namespace Erlang.Tests;

internal static class SpawnPrioritySynchronousBorrowHostChecks
{
    internal static async Task SameTurnCallbackStaysSynchronous()
    {
        await using var runtime = new ProcessRuntime(1);
        var observed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.Target,
            0,
            (_, _) => ValueTask.FromResult<Term>(Term.A(SpawnPriorityLifecycleHostNames.Done))
        );
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.Bridge,
            0,
            (context, _) =>
            {
                var pending = runtime.Modules.Call(context, SpawnPriorityLifecycleHostNames.Module,
                    SpawnPriorityLifecycleHostNames.Target);
                observed.TrySetResult(pending.IsCompletedSuccessfully);

                return pending;
            }
        );
        var parent = runtime.Spawn(context => runtime.Modules.Call(context,
            SpawnPriorityLifecycleHostNames.Module, SpawnPriorityLifecycleHostNames.Bridge));
        bool synchronous = await observed.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        _ = await parent.Completion.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        if (!synchronous)
            throw new InvalidOperationException(SpawnPriorityLifecycleHostNames.Failure);
    }
}
