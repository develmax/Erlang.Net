using System.Threading.Tasks;

namespace Erlang.Tests;

internal static class SpawnPriorityGuestReentryHostChecks
{
    // Source-only positive control for escaped registered-body admission.
    internal static async Task EscapedCallbackMustReadmitOwner()
    {
        await using var runtime = new ProcessRuntime(1);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selected = new TaskCompletionSource<TaskScheduler>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskScheduler? owner = null;
        runtime.Modules.Register(
            SpawnPriorityReentryNames.Module,
            SpawnPriorityReentryNames.Target,
            0,
            (_, _) =>
        {
            selected.TrySetResult(TaskScheduler.Current);

            return ValueTask.FromResult<Term>(Term.A(SpawnPriorityReentryNames.Done));
        }
        );
        runtime.Modules.Register(
            SpawnPriorityReentryNames.Module,
            SpawnPriorityReentryNames.Bridge,
            0,
            async (context, _) =>
        {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);

            return await runtime.Modules.Call(context, SpawnPriorityReentryNames.Module, SpawnPriorityReentryNames.Target);
        }
        );
        var parent = runtime.Spawn(async context =>
        {
            owner = TaskScheduler.Current;

            return await runtime.Modules.Call(context, SpawnPriorityReentryNames.Module, SpawnPriorityReentryNames.Bridge);
        });
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityReentryNames.TimeoutSeconds));
            await Task.Run(() => release.TrySetResult());
            var actual = await selected.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityReentryNames.TimeoutSeconds));
            _ = await parent.Completion.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityReentryNames.TimeoutSeconds));
            if (!ReferenceEquals(actual, owner))
                throw new InvalidOperationException(SpawnPriorityReentryNames.MissingAdmission);
        }
        finally
        {
            release.TrySetResult();
        }
    }
}
