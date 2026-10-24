using Erlang;

namespace Erlang.Tests;

internal static class HostDistributionAdditionalChecks
{
    internal static async Task Run()
    {
        await OwnershipAndInputTransfer();
        await QueueLimitsAndRaces();
        await SegmentLimits();
        await DeathAndReplacement();
        await HaltAndRuntimeDisposal();
    }

    private static async Task OwnershipAndInputTransfer()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreignRuntime = new ProcessRuntime(HostDistributionTestNames.ForeignNode);
        using var manager = NewManager(runtime);
        await using var controller = new HostDistributionTestActor(runtime);
        await using var input = new HostDistributionTestActor(runtime);
        await using var foreign = new HostDistributionTestActor(foreignRuntime);
        var handle = await controller.Invoke(context => manager.Attach(HostDistributionTestNames.Channel, context));
        await input.Invoke(context => Reject(() => manager.GetSize(handle, context)));
        await foreign.Invoke(context => Reject(() => manager.GetSize(handle, context)));
        await foreign.Invoke(context => Reject(() => manager.Attach(HostDistributionTestNames.OtherChannel, context)));
        await controller.Invoke(context =>
        {
            manager.SetInputHandler(handle, context, input.Process.Pid);

            return true;
        });
        await controller.Invoke(context => Reject(() => manager.RecordAcceptedHostInput(handle, context)));
        await input.Invoke(context =>
        {
            manager.RecordAcceptedHostInput(handle, context);

            return true;
        });
        await foreign.Invoke(context => Reject(() => manager.RecordAcceptedHostInput(handle, context)));
        Check(manager.Snapshot(handle).AcceptedInputPackets == 1);
        await controller.Invoke(context => Reject(() => manager.SetInputHandler(handle, context, foreign.Process.Pid)));
        Check(manager.Snapshot(handle).InputHandler.Equals(input.Process.Pid));
    }

    private static async Task QueueLimitsAndRaces()
    {
        await using var runtime = new ProcessRuntime();
        using var manager = NewManager(runtime);
        await using var controller = new HostDistributionTestActor(runtime);
        var handle = await controller.Invoke(context => manager.Attach(HostDistributionTestNames.Channel, context));
        var empty = new HostDistributionBuffer([]);
        Check(manager.TryEnqueueFinalizedHostBuffer(handle, empty));
        Check(manager.TryEnqueueFinalizedHostBuffer(handle, empty));
        Check(!manager.TryEnqueueFinalizedHostBuffer(handle, empty));
        Check(manager.Snapshot(handle).QueuedPayloadBytes == 0);
        await controller.Invoke(context =>
        {
            Check(manager.TakeFinalizedHostBuffer(handle, context, out _) == empty);
            Check(manager.TakeFinalizedHostBuffer(handle, context, out _) == empty);

            return true;
        });
        var full = new HostDistributionBuffer([new byte[HostDistributionTestNames.PayloadLimit].AsMemory()]);
        Check(manager.TryEnqueueFinalizedHostBuffer(handle, full));
        Check(!manager.TryEnqueueFinalizedHostBuffer(handle, new HostDistributionBuffer([new byte[1].AsMemory()])));
        await controller.Invoke(context => manager.TakeFinalizedHostBuffer(handle, context, out _));
        var packet = new HostDistributionBuffer([new byte[] { 7 }.AsMemory()]);
        Term? firstNotice = null;
        for (int iteration = 0; iteration < HostDistributionTestNames.RaceRepetitions; iteration++)
        {
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var armReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var enqueueReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var arm = controller.InvokeAsync(async context =>
            {
                armReady.SetResult();
                await start.Task;
                manager.ArmNotification(handle, context);

                return true;
            });
            var enqueue = Task.Run(async () =>
            {
                enqueueReady.SetResult();
                await start.Task;

                return manager.TryEnqueueFinalizedHostBuffer(handle, packet);
            });
            await Task.WhenAll(armReady.Task, enqueueReady.Task).WaitAsync(HostDistributionTestNames.Deadline);
            start.SetResult();
            await Task.WhenAll(arm, enqueue).WaitAsync(HostDistributionTestNames.Deadline);
            Check(await enqueue);
            await controller.InvokeAsync(async context =>
            {
                var notice = await context.ReceiveAsync(
                    term => term is Atom { Name: HostDistributionTestNames.Data } ? term : null,
                    HostDistributionTestNames.Deadline
                );
                Check(notice is Atom);
                if (firstNotice is null)
                    firstNotice = notice;
                else
                    Check(ReferenceEquals(firstNotice, notice));
                Check(context.Mailbox.Count == 0);
                Check(manager.TakeFinalizedHostBuffer(handle, context, out _) == packet);
                Check(manager.TakeFinalizedHostBuffer(handle, context, out _) is null);

                return true;
            });
        }
        Check(manager.Snapshot(handle).QueuedPayloadBytes == 0);
    }

    private static async Task DeathAndReplacement()
    {
        await using var runtime = new ProcessRuntime();
        using var manager = NewManager(runtime);
        await using var controller = new HostDistributionTestActor(runtime);
        await using var sender = new HostDistributionTestActor(runtime);
        var handle = await controller.Invoke(context => manager.Attach(HostDistributionTestNames.Channel, context));
        using var lease = manager.Acquire(handle);
        Check(manager.TryEnqueueFinalizedHostBuffer(handle, new HostDistributionBuffer([new byte[] { 1 }.AsMemory()])));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var blocked = controller.InvokeAsync(async context =>
        {
            entered.SetResult();
            await release.Task;

            return Reject(() => manager.GetSize(handle, context));
        });
        await entered.Task.WaitAsync(HostDistributionTestNames.Deadline);
        try
        {
            await sender.Invoke(context =>
            {
                runtime.Exit(context, controller.Process.Pid, Term.A(HostDistributionTestNames.Killed));

                return true;
            });
            await controller.Process.Completion.WaitAsync(HostDistributionTestNames.Deadline);
            Check(Reject(() => lease.Snapshot()));
            await using var replacement = new HostDistributionTestActor(runtime);
            var newer = await replacement.Invoke(context => manager.Attach(HostDistributionTestNames.Channel, context));
            Check(newer.Generation > handle.Generation);
            Check(manager.Snapshot(newer).QueuedPayloadBytes == 0);
            manager.Close(handle);
            release.SetResult();
            Check(await blocked);
            Check(manager.Snapshot(newer).QueuedBuffers == 0);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    private static async Task HaltAndRuntimeDisposal()
    {
        await using var runtime = new ProcessRuntime();
        using var manager = NewManager(runtime);
        await using var controller = new HostDistributionTestActor(runtime);
        var handle = await controller.Invoke(context => manager.Attach(HostDistributionTestNames.Channel, context));
        using var lease = manager.Acquire(handle);
        var halt = runtime.RequestImmediateHalt(0);
        Check(Reject(() => lease.Snapshot()));
        await halt.WaitAsync(HostDistributionTestNames.Deadline);
        await runtime.HaltQuiescence.WaitAsync(HostDistributionTestNames.Deadline);
        await using var disposedRuntime = new ProcessRuntime();
        using var disposedManager = NewManager(disposedRuntime);
        await using var disposedController = new HostDistributionTestActor(disposedRuntime);
        var disposedHandle = await disposedController.Invoke(context => disposedManager.Attach(HostDistributionTestNames.Channel, context));
        await disposedRuntime.DisposeAsync();
        Check(Reject(() => disposedManager.Snapshot(disposedHandle)));
        disposedManager.Dispose();
    }

    private static HostDistribution NewManager(ProcessRuntime runtime)
        => new(
            runtime,
            HostDistributionTestNames.PayloadLimit,
            HostDistributionTestNames.BufferLimit,
            HostDistributionTestNames.SegmentLimit
        );

    private static async Task SegmentLimits()
    {
        await using var runtime = new ProcessRuntime();
        using var manager = NewManager(runtime);
        await using var controller = new HostDistributionTestActor(runtime);
        var handle = await controller.Invoke(context => manager.Attach(HostDistributionTestNames.Channel, context));
        var boundary = new HostDistributionBuffer(Enumerable.Repeat(ReadOnlyMemory<byte>.Empty, HostDistributionTestNames.SegmentLimit));
        Check(boundary.SegmentCount == HostDistributionTestNames.SegmentLimit && boundary.PayloadBytes == 0);
        Check(RejectArgument(() => new HostDistributionBuffer(Enumerable.Repeat(ReadOnlyMemory<byte>.Empty, HostDistributionTestNames.OverSegmentLimit))));
        Check(manager.TryEnqueueFinalizedHostBuffer(handle, boundary));
        var one = new HostDistributionBuffer([ReadOnlyMemory<byte>.Empty]);
        Check(!manager.TryEnqueueFinalizedHostBuffer(handle, one));
        var snapshot = manager.Snapshot(handle);
        Check(snapshot.QueuedPayloadBytes == 0 && snapshot.QueuedBuffers == 1 && snapshot.QueuedSegments == HostDistributionTestNames.SegmentLimit);
        await controller.Invoke(context => manager.TakeFinalizedHostBuffer(handle, context, out _));
        Check(manager.Snapshot(handle).QueuedSegments == 0);
        var half = new HostDistributionBuffer(Enumerable.Repeat(ReadOnlyMemory<byte>.Empty, HostDistributionTestNames.HalfSegmentLimit));
        Check(manager.TryEnqueueFinalizedHostBuffer(handle, half));
        Check(manager.TryEnqueueFinalizedHostBuffer(handle, half));
        Check(!manager.TryEnqueueFinalizedHostBuffer(handle, one));
        Check(manager.Snapshot(handle).QueuedSegments == HostDistributionTestNames.SegmentLimit);
        manager.Close(handle);
        Check(Reject(() => manager.Snapshot(handle)));
        var replacement = await controller.Invoke(context => manager.Attach(HostDistributionTestNames.Channel, context));
        Check(manager.Snapshot(replacement).QueuedSegments == 0);
        Check(manager.TryEnqueueFinalizedHostBuffer(replacement, boundary));
        manager.Dispose();
        Check(Reject(() => manager.Snapshot(replacement)));
    }

    private static bool RejectArgument(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return true;
        }

        throw new Exception(HostDistributionTestNames.Failure);
    }

    private static bool Reject(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return true;
        }

        throw new Exception(HostDistributionTestNames.Failure);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(HostDistributionTestNames.Failure);
    }
}
