using System.Collections.Concurrent;
using System.Reflection;

namespace Erlang.Tests;

internal static class SpawnPriorityQueuedChangeHostChecks
{
    internal static async Task ReprioritizesAlreadyQueuedOwner()
    {
        await using var runtime = new ProcessRuntime(1);
        var order = new ConcurrentQueue<string>();
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            SpawnPriorityLifecycleHostNames.Module,
            SpawnPriorityLifecycleHostNames.Record,
            1,
            async (context, arguments) =>
            {
                string label = ((Atom)arguments[0]).Name;
                order.Enqueue(label);
                if (label == SpawnPriorityLifecycleHostNames.Promoted)
                {
                    var actual = await runtime.Modules.Call(
                        context,
                        SpawnPriorityLifecycleHostNames.Erlang,
                        SpawnPriorityLifecycleHostNames.ProcessInfo,
                        context.Self,
                        Term.A(SpawnPriorityLifecycleHostNames.Priority)
                    );
                    changed.TrySetResult(actual.Equals(Term.Tuple(Term.A(SpawnPriorityLifecycleHostNames.Priority),
                        Term.A(SpawnPriorityLifecycleHostNames.High))));
                }
                if (order.Count == SpawnPriorityLifecycleHostNames.QueuedChangeChildren)
                    done.TrySetResult();

                return Term.A(SpawnPriorityLifecycleHostNames.Done);
            }
        );
        var parent = runtime.Spawn(async context =>
        {
            Pid? promoted = null;
            foreach (string level in new[] { SpawnPriorityLifecycleHostNames.Low, SpawnPriorityLifecycleHostNames.Normal })
            {
                string label = level == SpawnPriorityLifecycleHostNames.Low
                    ? SpawnPriorityLifecycleHostNames.Promoted : SpawnPriorityLifecycleHostNames.Normal;
                var pending = runtime.Modules.Call(
                    context,
                    SpawnPriorityLifecycleHostNames.Erlang,
                    SpawnPriorityLifecycleHostNames.Spawn,
                    Term.A(SpawnPriorityLifecycleHostNames.Module),
                    Term.A(SpawnPriorityLifecycleHostNames.Record),
                    Term.List(Term.A(label)),
                    Term.List(Term.Tuple(Term.A(SpawnPriorityLifecycleHostNames.Priority), Term.A(level)))
                );
                if (!pending.IsCompletedSuccessfully)
                    throw new InvalidOperationException(SpawnPriorityLifecycleHostNames.Failure);
                var value = await pending;
                if (level == SpawnPriorityLifecycleHostNames.Low)
                    promoted = (Pid)value;
            }
            // Internal queue-unit control only: OTP process_flag/2 changes self,
            // so this does not claim a new public cross-process setter.
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            object gate = typeof(ProcessRuntime).GetField(SpawnPriorityLifecycleHostNames.GateField, flags)!.GetValue(runtime)!;
            lock (gate)
            {
                var contexts = (Dictionary<Pid, ProcessContext>)typeof(ProcessRuntime)
                    .GetField(SpawnPriorityLifecycleHostNames.ProcessesField, flags)!.GetValue(runtime)!;
                _ = typeof(ProcessRuntime).GetMethod(SpawnPriorityLifecycleHostNames.ChangePriorityMethod, flags)!
                    .Invoke(runtime, [contexts[promoted!], Term.A(SpawnPriorityLifecycleHostNames.High)]);
            }

            return Term.A(SpawnPriorityLifecycleHostNames.Done);
        });
        _ = await parent.Completion.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        await done.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        bool high = await changed.Task.WaitAsync(TimeSpan.FromSeconds(SpawnPriorityLifecycleHostNames.TimeoutSeconds));
        if (!high || string.Join(SpawnPriorityLifecycleHostNames.Delimiter, order) != SpawnPriorityLifecycleHostNames.PromotedOrder)
            throw new InvalidOperationException(SpawnPriorityLifecycleHostNames.Failure);
    }
}
