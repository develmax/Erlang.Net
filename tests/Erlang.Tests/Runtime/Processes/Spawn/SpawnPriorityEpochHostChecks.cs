using System.Collections.Concurrent;

namespace Erlang.Tests;

internal static class SpawnPriorityEpochHostChecks
{
    internal static async Task LowPaysOncePerLogicalReduction()
    {
        await using var runtime = new ProcessRuntime(1);
        var order = new ConcurrentQueue<string>();
        var lowRan = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var normalFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.Low,
            0,
            (_, _) =>
            {
                order.Enqueue(SpawnPriorityLifecycleHostNames.LowMark);
                lowRan.TrySetResult();

                return ValueTask.FromResult<Term>(Term.A(SpawnPriorityLifecycleHostNames.Done));
            }
        );
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.Normal,
            0,
            async (context, _) =>
            {
                for (int index = 0; index < SpawnPriorityLifecycleHostNames.LogicalSlices; index++)
                {
                    order.Enqueue(SpawnPriorityLifecycleHostNames.NormalMark);
                    await runtime.Modules.Call(
                        context,
                        SpawnPriorityLifecycleHostNames.Erlang,
                        SpawnPriorityLifecycleHostNames.Bump,
                        Term.I(SpawnPriorityLifecycleHostNames.MaximumBump)
                    );
                }
                normalFinished.TrySetResult();

                return Term.A(SpawnPriorityLifecycleHostNames.Done);
            }
        );
        var parent = runtime.Spawn(async context =>
        {
            foreach (string level in new[] { SpawnPriorityLifecycleHostNames.Low, SpawnPriorityLifecycleHostNames.Normal })
            {
                var pending = runtime.Modules.Call(
                    context,
                    SpawnPriorityLifecycleHostNames.Erlang,
                    SpawnPriorityLifecycleHostNames.Spawn,
                    Term.A(SpawnPriorityLifecycleHostNames.Module),
                    Term.A(level),
                    Nil.Value,
                    Term.List(Term.Tuple(Term.A(SpawnPriorityLifecycleHostNames.Priority), Term.A(level)))
                );
                if (!pending.IsCompletedSuccessfully)
                    throw new InvalidOperationException(SpawnPriorityLifecycleHostNames.Failure);
                _ = await pending;
            }

            return Term.A(SpawnPriorityLifecycleHostNames.Done);
        });
        _ = await parent.Completion.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        await lowRan.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        await normalFinished.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        int firstLow = Array.IndexOf(order.ToArray(), SpawnPriorityLifecycleHostNames.LowMark);
        if (firstLow != SpawnPriorityLifecycleHostNames.ExpectedNormalBeforeLow)
            throw new InvalidOperationException(SpawnPriorityLifecycleHostNames.Failure);
    }
}
