using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class ManagedCollectorPrestart415Checks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(maximumActorConcurrency: 1);
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
            // Occupy the sole actual actor worker; source/trace preparation
            // can now run outside R while the collector genuinely cannot start.
            using var release = new ManualResetEventSlim();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle blocker = runtime.Spawn(context =>
            {
                entered.TrySetResult();
                if (!release.Wait(ManagedCollectors413Names.Deadline, context.Cancellation))
                    throw new TimeoutException();

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            try
            {
                await entered.Task.WaitAsync(ManagedCollectors413Names.Deadline);
                first = ManagedCollectors413Access.Spawn(runtime, parent, item.Item1);
                lock (gate)
                {
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
                    TerminateActual415(runtime, child);
                    RequireCleared(slot);
                }
            }
            finally
            {
                // Never leave the actual scheduler blocked after an assertion,
                // preparation failure, cancellation or timeout.
                release.Set();
                await blocker.Completion.WaitAsync(ManagedCollectors413Names.Deadline);
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
                TerminateActual415(runtime, replacementActor);
                RequireCleared(slot);
            }
            await replacement.Completion.WaitAsync(ManagedCollectors413Names.Deadline);
            await ((TaskCompletionSource)ManagedCollectors413Access.Property(
                replacementActor, ManagedCollectorPrestart415Names.Unwound)!).Task.WaitAsync(ManagedCollectors413Names.Deadline);
        }
    }

    private static void TerminateActual415(ProcessRuntime runtime, ProcessContext child)
    {
        MethodInfo terminate = typeof(ProcessRuntime).GetMethods(ManagedCollectors413Names.Members)
            .Single(method => method.Name == ManagedCollectorPrestart415Names.Terminate
                && method.GetParameters().Length == ManagedCollectorPrestart415Names.TerminateArguments);
        try
        {
            terminate.Invoke(runtime, [child, Term.A(ManagedCollectorPrestart415Names.Reason), null, null]);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } actual)
        {
            ExceptionDispatchInfo.Capture(actual).Throw();
            throw;
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
