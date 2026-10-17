using System.Collections.Concurrent;

namespace Erlang.Tests;

internal static class SpawnPriorityWaitingHostChecks
{
    internal static async Task WaitingHighDoesNotHoldCapacity()
    {
        await using var runtime = new ProcessRuntime(1);
        var order = new ConcurrentQueue<string>();
        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Pid? high = null;
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.High,
            0,
            async (context, arguments) =>
            {
                order.Enqueue(SpawnPriorityLifecycleHostNames.Started);
                var selected = await context.ReceiveAsync(
                    message => message is Atom { Name: SpawnPriorityLifecycleHostNames.Release } ? message : null,
                    TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds)
                );
                if (selected is null)
                    throw new InvalidOperationException(SpawnPriorityLifecycleHostNames.Failure);
                order.Enqueue(SpawnPriorityLifecycleHostNames.Ended);
                ended.TrySetResult();

                return Term.A(SpawnPriorityLifecycleHostNames.Done);
            }
        );
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.Normal,
            0,
            async (context, arguments) =>
            {
                order.Enqueue(SpawnPriorityLifecycleHostNames.NormalMark);

                return await runtime.Modules.Call(
                    context,
                    SpawnPriorityLifecycleHostNames.Erlang,
                    SpawnPriorityLifecycleHostNames.Send,
                    high!,
                    Term.A(SpawnPriorityLifecycleHostNames.Release)
                );
            }
        );
        var parent = runtime.Spawn(async context =>
        {
            foreach (string level in new[] { SpawnPriorityLifecycleHostNames.High, SpawnPriorityLifecycleHostNames.Normal })
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
                var value = await pending;
                if (level == SpawnPriorityLifecycleHostNames.High)
                    high = (Pid)value;
            }

            return Term.A(SpawnPriorityLifecycleHostNames.Done);
        });
        _ = await parent.Completion.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        await ended.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        if (string.Join(SpawnPriorityLifecycleHostNames.Delimiter, order) != SpawnPriorityLifecycleHostNames.HighWaitOrder)
            throw new InvalidOperationException(SpawnPriorityLifecycleHostNames.Failure);
    }
}
