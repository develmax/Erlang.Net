using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Erlang.Tests;

internal static class DebugDirtyQueue424Access
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static object Field(object value, string name)
        => value.GetType().GetField(name, Members)!.GetValue(value)!;

    private static object Property(object value, string name)
        => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    internal static bool QueuedDirty(ProcessRuntime runtime, ProcessContext target, string kind)
    {
        object? owner = typeof(ProcessRuntime).GetField("dirtyNative419", Members)!.GetValue(runtime);
        if (owner is null) return false;
        lock (Field(owner, "gate"))
        {
            object worker = Field(owner, kind);
            foreach (object call in (IEnumerable)Property(worker, "Queue"))
                if (ReferenceEquals(Property(call, "Owner"), target)) return true;

            return false;
        }
    }

    internal static bool PhysicallyDirty(ProcessRuntime runtime, ProcessContext target)
    {
        object owner = Field(runtime, "dirtyNative419");

        return (bool)owner.GetType().GetMethod("IsExecuting423", Members)!.Invoke(owner, [target])!;
    }

    internal static (Task<Term> Outcome, Task Settled) PhysicalWork(ProcessRuntime runtime, ProcessContext target)
    {
        object owner = Field(runtime, "dirtyNative419");
        lock (Field(owner, "gate"))
        {
            object call = ((IEnumerable)Field(owner, "invocations")).Cast<object>()
                .Single(value => ReferenceEquals(Property(value, "Owner"), target)
                    && (bool)Property(value, "PhysicallyExecuting423"));

            return (((TaskCompletionSource<Term>)Property(call, "Outcome")).Task,
                ((TaskCompletionSource)Property(call, "Settled")).Task);
        }
    }

    internal static bool QueuedNormal(ProcessRuntime runtime, ProcessContext target)
    {
        object dispatcher = Field(runtime, "actorDispatcher");
        lock (Field(dispatcher, "gate"))
        {
            object scheduler = Property(target, "ActorTaskScheduler");

            return ((ICollection)Property(scheduler, "Pending")).Count != 0;
        }
    }

    internal static async Task Until(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed > DebugDirty424Names.Deadline) throw new TimeoutException(DebugDirty424Names.Failure);
            await Task.Delay(1);
        }
    }
}
