using System.Collections.Concurrent;

namespace Erlang.Tests;

internal static class SpawnPriorityAdmissionHostChecks
{
    internal static async Task AllReadyPriorityOrder()
    {
        await using var runtime = new ProcessRuntime(1);
        var observed = new ConcurrentQueue<string>();
        var recorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            SpawnPriorityHostNames.Module,
            SpawnPriorityHostNames.Record,
            1,
            (_, arguments) =>
        {
            observed.Enqueue(((Atom)arguments[0]).Name);
            if (observed.Count == SpawnPriorityHostNames.ExpectedChildren)
                recorded.TrySetResult();

            return ValueTask.FromResult<Term>(Term.A(SpawnPriorityHostNames.Done));
        }
        );
        var parent = runtime.Spawn(async context =>
        {
            // Local spawn must complete synchronously; all four children are queued
            // before this capacity-one actor returns. This is not a latency test.
            foreach (string level in SpawnPriorityHostNames.Levels)
            {
                var pending = runtime.Modules.Call(
                    context,
                    SpawnPriorityHostNames.Erlang,
                    SpawnPriorityHostNames.SpawnOpt,
                    Term.A(SpawnPriorityHostNames.Module),
                    Term.A(SpawnPriorityHostNames.Record),
                    Term.List(Term.A(level)),
                    Term.List(Term.Tuple(Term.A(SpawnPriorityHostNames.Priority), Term.A(level)))
                );
                if (!pending.IsCompletedSuccessfully)
                    throw new InvalidOperationException(SpawnPriorityHostNames.Unexpected);
                _ = await pending;
            }

            return Term.A(SpawnPriorityHostNames.Done);
        });
        _ = await parent.Completion.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityHostNames.TimeoutSeconds));
        await recorded.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityHostNames.TimeoutSeconds));
        string actual = string.Join(SpawnPriorityHostNames.Delimiter, observed);
        if (actual != SpawnPriorityHostNames.Sequence)
            throw new InvalidOperationException(SpawnPriorityHostNames.Unexpected + actual);
    }
}
