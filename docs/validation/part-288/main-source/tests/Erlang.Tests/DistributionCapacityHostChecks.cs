using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class DistributionCapacityHostChecks
{
    internal static async Task Run()
    {
        await CheckDrainAndSubscription();
        await CheckDimensions();
        await CheckRetirement();
        await CheckControllerDeath();
    }

    private static async Task CheckDrainAndSubscription()
    {
        await using var runtime = new ProcessRuntime();
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        using var queue = Create(runtime);
        HostDistributionHandle handle = queue.Attach(DistributionCapacityHostNames.Channel, await controller.Ready);
        var full = Buffer(DistributionCapacityHostNames.ByteLimit);
        var next = Buffer(1);
        Check(queue.TryEnqueueFinalizedHostBuffer(handle, full));
        Task first = Wait(queue, handle, next);
        Task second = Wait(queue, handle, next);
        object queueGate = typeof(HostDistribution).GetField(DistributionCapacityHostNames.QueueGate,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(queue)!;
        Task<bool> continuationHeldGate = first.ContinueWith(
            _ => Monitor.IsEntered(queueGate),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
        Check(!first.IsCompleted && !second.IsCompleted);
        Check(!queue.TryEnqueueFinalizedHostBuffer(handle, next));
        Check(queue.Snapshot(handle).QueuedPayloadBytes == DistributionCapacityHostNames.ByteLimit);
        using var cancellation = new CancellationTokenSource();
        Task canceledObserver = first.WaitAsync(cancellation.Token);
        cancellation.Cancel();
        await Canceled(canceledObserver);
        Check(!first.IsCompleted);
        await controller.Invoke(context => queue.TakeFinalizedHostBuffer(handle, context, out _));
        await Task.WhenAll(first, second).WaitAsync(DistributionCapacityHostNames.Deadline);
        Check(!await continuationHeldGate.WaitAsync(DistributionCapacityHostNames.Deadline));
        Check(queue.Snapshot(handle).QueuedPayloadBytes == 0);
        Check(queue.TryEnqueueFinalizedHostBuffer(handle, full));
        Task later = Wait(queue, handle, next);
        Check(!later.IsCompleted);
        await controller.Invoke(context => queue.TakeFinalizedHostBuffer(handle, context, out _));
        await later.WaitAsync(DistributionCapacityHostNames.Deadline);
        // A dequeue between a failed admission and subscription cannot lose the wake.
        Check(Wait(queue, handle, next).IsCompletedSuccessfully);
        Check(queue.Snapshot(handle).ConsumedOutputPackets == 2);
        Check(queue.TryEnqueueFinalizedHostBuffer(handle, next));
    }

    private static async Task CheckDimensions()
    {
        await using var runtime = new ProcessRuntime();
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext owner = await controller.Ready;
        foreach (bool segments in new[] { false, true })
        {
            using var queue = new HostDistribution(
                runtime,
                DistributionCapacityHostNames.ByteLimit,
                segments ? DistributionCapacityHostNames.BufferLimit : 1,
                segments ? 1 : DistributionCapacityHostNames.SegmentLimit
            );
            HostDistributionHandle handle = queue.Attach(DistributionCapacityHostNames.Channel, owner);
            Check(queue.TryEnqueueFinalizedHostBuffer(handle, Buffer(1)));
            Task wake = Wait(queue, handle, Buffer(1));
            Check(!wake.IsCompleted);
            await controller.Invoke(context => queue.TakeFinalizedHostBuffer(handle, context, out _));
            await wake.WaitAsync(DistributionCapacityHostNames.Deadline);
            ExpectImpossible(() => Wait(queue, handle, Buffer(DistributionCapacityHostNames.ByteLimit + 1)));
            var tooManySegments = new HostDistributionBuffer(new ReadOnlyMemory<byte>[]
            {
                new byte[1], new byte[1], new byte[1]
            });
            ExpectImpossible(() => Wait(queue, handle, tooManySegments));
        }
    }

    private static async Task CheckRetirement()
    {
        await using var runtime = new ProcessRuntime();
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        using var queue = Create(runtime);
        ProcessContext owner = await controller.Ready;
        HostDistributionHandle old = queue.Attach(DistributionCapacityHostNames.Channel, owner);
        Check(queue.TryEnqueueFinalizedHostBuffer(old, Buffer(DistributionCapacityHostNames.ByteLimit)));
        Task retired = Wait(queue, old, Buffer(1));
        queue.Close(old);
        await Canceled(retired);
        HostDistributionHandle replacement = queue.Attach(DistributionCapacityHostNames.Channel, owner);
        Check(queue.TryEnqueueFinalizedHostBuffer(replacement, Buffer(DistributionCapacityHostNames.ByteLimit)));
        Task current = Wait(queue, replacement, Buffer(1));
        queue.Close(old);
        Check(!current.IsCompleted);
        queue.Dispose();
        await Canceled(current);
    }

    private static async Task CheckControllerDeath()
    {
        await using var runtime = new ProcessRuntime();
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        using var queue = Create(runtime);
        HostDistributionHandle handle = queue.Attach(DistributionCapacityHostNames.Channel, await controller.Ready);
        Check(queue.TryEnqueueFinalizedHostBuffer(handle, Buffer(DistributionCapacityHostNames.ByteLimit)));
        Task pending = Wait(queue, handle, Buffer(1));
        await controller.DisposeAsync();
        await Canceled(pending);
    }

    private static HostDistribution Create(ProcessRuntime runtime)
        => new(
            runtime,
            DistributionCapacityHostNames.ByteLimit,
            DistributionCapacityHostNames.BufferLimit,
            DistributionCapacityHostNames.SegmentLimit
        );

    private static HostDistributionBuffer Buffer(int bytes)
        => new([new byte[bytes].AsMemory()]);

    private static Task Wait(HostDistribution queue, HostDistributionHandle handle, HostDistributionBuffer buffer)
    {
        // Observe the internal prerequisite without changing queue or runtime state.
        MethodInfo method = typeof(HostDistribution).GetMethod(DistributionCapacityHostNames.WaitMethod,
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        return (Task)method.Invoke(queue, [handle, buffer])!;
    }

    private static void ExpectImpossible(Func<Task> action)
    {
        try
        {
            _ = action();
        }
        catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
        {
            return;
        }
        throw new InvalidOperationException(DistributionCapacityHostNames.Failure);
    }

    private static async Task Canceled(Task task)
    {
        try
        {
            await task.WaitAsync(DistributionCapacityHostNames.Deadline);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        throw new InvalidOperationException(DistributionCapacityHostNames.Failure);
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(DistributionCapacityHostNames.Failure);
    }
}
