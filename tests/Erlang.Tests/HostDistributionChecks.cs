using Erlang;

namespace Erlang.Tests;

internal static class HostDistributionChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        using var manager = new HostDistribution(
            runtime,
            HostDistributionTestNames.PayloadLimit,
            HostDistributionTestNames.BufferLimit,
            HostDistributionTestNames.SegmentLimit
        );
        using var other = new HostDistribution(
            runtime,
            HostDistributionTestNames.PayloadLimit,
            HostDistributionTestNames.BufferLimit,
            HostDistributionTestNames.SegmentLimit
        );
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(async context =>
        {
            ready.SetResult(context);
            await done.Task.WaitAsync(context.Cancellation);

            return Nil.Value;
        });
        var caller = await ready.Task.WaitAsync(HostDistributionTestNames.Deadline);
        var handle = manager.Attach(HostDistributionTestNames.Channel, caller);
        using var lease = manager.Acquire(handle);
        Check(!manager.GetSize(handle, caller));
        Check(!manager.SetGetSize(handle, caller, true));
        Check(manager.GetSize(handle, caller));
        Check(manager.SetGetSize(handle, caller, false));
        Reject(() => manager.Attach(HostDistributionTestNames.OtherChannel, caller));
        Reject(() => other.Snapshot(handle));
        manager.ArmNotification(handle, caller);
        manager.ArmNotification(handle, caller);
        Check(lease.Snapshot().NotificationArmed);
        byte[] bytes = [1, 2, 3];
        var buffer = new HostDistributionBuffer([bytes.AsMemory()]);
        bytes[0] = 9;
        Check(manager.TryEnqueueFinalizedHostBuffer(handle, buffer));
        Check(manager.TryEnqueueFinalizedHostBuffer(handle, buffer));
        Check(!manager.TryEnqueueFinalizedHostBuffer(handle, buffer));
        var notice = await caller.ReceiveAsync(
            term => term is Atom { Name: HostDistributionTestNames.Data } ? term : null,
            HostDistributionTestNames.Deadline
        );
        Check(notice is Atom);
        Check(caller.Mailbox.Count == 0);
        var snapshot = lease.Snapshot();
        Check(snapshot.QueuedBuffers == 2 && snapshot.QueuedPayloadBytes == 6 && !snapshot.NotificationArmed);
        Check(manager.TakeFinalizedHostBuffer(handle, caller, out bool sized) == buffer && !sized);
        Check(buffer.Segments[0].ToArray().SequenceEqual(new byte[] { 1, 2, 3 }));
        byte[] exported = buffer.Segments[0].ToArray();
        exported[0] = 9;
        Check(buffer.Segments[0].ToArray().SequenceEqual(new byte[] { 1, 2, 3 }));
        Check(manager.TakeFinalizedHostBuffer(handle, caller, out _) == buffer);
        Check(manager.TakeFinalizedHostBuffer(handle, caller, out _) is null);
        manager.RecordAcceptedHostInput(handle, caller);
        snapshot = lease.Snapshot();
        Check(snapshot.AcceptedInputPackets == 1 && snapshot.ConsumedOutputPackets == 2 && snapshot.QueuedPayloadBytes == 0);
        manager.Close(handle);
        Reject(() => lease.Snapshot());
        var replacement = manager.Attach(HostDistributionTestNames.Channel, caller);
        Check(replacement.Generation > handle.Generation);
        manager.Close(handle);
        Check(manager.Snapshot(replacement).QueuedBuffers == 0);
        lease.Dispose();
        RejectDisposed(() => lease.Snapshot());
        done.SetResult();
        await process.Completion.WaitAsync(HostDistributionTestNames.Deadline);
        Reject(() => manager.Snapshot(replacement));
        manager.Dispose();
        await HostDistributionAdditionalChecks.Run();
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(HostDistributionTestNames.Failure);
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new Exception(HostDistributionTestNames.Failure);
    }

    private static void RejectDisposed(Action action)
    {
        try
        {
            action();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        throw new Exception(HostDistributionTestNames.Failure);
    }
}
