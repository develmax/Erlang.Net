using System.Collections;

namespace Erlang.Tests;

internal static class ManagedCollectorPrestart415Checks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await ManagedCollectors413Access.Initialize(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext parent = await owner.Ready;
        object gate = ManagedCollectors413Access.Field(runtime, ManagedCollectorPrestart415Names.Gate)!;
        foreach (var item in new[]
        {
            (ManagedCollectors413Names.Literal, ManagedCollectors413Names.LiteralSlot),
            (ManagedCollectors413Names.Trace, ManagedCollectors413Names.TraceSlot)
        })
        {
            object? expectedSource = runtime.Modules.SourceOwner(item.Item1, ManagedCollectors413Names.Start, 0);
            ProcessHandle first;
            ProcessContext child;
            object slot;
            lock (gate)
            {
                first = ManagedCollectors413Access.Spawn(runtime, parent, item.Item1);
                child = (ProcessContext)((IDictionary)ManagedCollectors413Access.Field(
                    runtime, ManagedCollectorPrestart415Names.Processes)!)[first.Pid]!;
                slot = ManagedCollectors413Access.Field(runtime, item.Item2)!;
                ManagedCollectors413Access.Require(!(bool)ManagedCollectors413Access.Field(
                    child, ManagedCollectorPrestart415Names.ExecutionStarted)!);
                ManagedCollectors413Access.Require(ReferenceEquals(
                    ManagedCollectors413Access.Property(slot, ManagedCollectors413Names.Actor), child));
                ManagedCollectors413Access.Require(ReferenceEquals(
                    ManagedCollectors413Access.Property(slot, ManagedCollectors413Names.Source),
                    expectedSource));
                ManagedCollectors413Access.Invoke(
                    runtime,
                    typeof(ProcessRuntime),
                    ManagedCollectorPrestart415Names.Terminate,
                    child,
                    Term.A(ManagedCollectorPrestart415Names.Reason)
                );
                RequireCleared(slot);
            }
            await first.Completion.WaitAsync(ManagedCollectors413Names.Deadline);
            await ((TaskCompletionSource)ManagedCollectors413Access.Property(
                child, ManagedCollectorPrestart415Names.Unwound)!).Task.WaitAsync(ManagedCollectors413Names.Deadline);
            ProcessHandle replacement = ManagedCollectors413Access.Spawn(runtime, parent, item.Item1);
            ProcessContext replacementActor = await ManagedCollectors413Access.Role(runtime, item.Item2);
            ManagedCollectors413Access.Require(!replacement.Pid.Equals(first.Pid));
            lock (gate)
            {
                ManagedCollectors413Access.Require(ReferenceEquals(
                    ManagedCollectors413Access.Property(slot, ManagedCollectors413Names.Actor), replacementActor));
                ManagedCollectors413Access.Invoke(
                    runtime,
                    typeof(ProcessRuntime),
                    ManagedCollectorPrestart415Names.Terminate,
                    replacementActor,
                    Term.A(ManagedCollectorPrestart415Names.Reason)
                );
                RequireCleared(slot);
            }
            await replacement.Completion.WaitAsync(ManagedCollectors413Names.Deadline);
            await ((TaskCompletionSource)ManagedCollectors413Access.Property(
                replacementActor, ManagedCollectorPrestart415Names.Unwound)!).Task.WaitAsync(ManagedCollectors413Names.Deadline);
        }
    }

    private static void RequireCleared(object slot)
    {
        foreach (string name in new[]
        {
            ManagedCollectors413Names.Actor,
            ManagedCollectors413Names.Source,
            ManagedCollectorPrestart415Names.Reservation
        })
            ManagedCollectors413Access.Require(ManagedCollectors413Access.Property(slot, name) is null);
    }
}
