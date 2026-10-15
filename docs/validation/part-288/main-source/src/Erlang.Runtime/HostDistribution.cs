namespace Erlang;

public sealed class HostDistribution : IDisposable
{
    private readonly object gate = new();
    private readonly ProcessRuntime runtime;
    private readonly long maximumQueuedPayloadBytes;
    private readonly int maximumQueuedBuffers;
    private readonly int maximumQueuedSegments;
    private readonly Dictionary<string, HostDistributionEntry> entries = new(StringComparer.Ordinal);
    private readonly Dictionary<Pid, HostDistributionEntry> controllers = new();
    private ulong generation;
    private bool closed;

    public HostDistribution(
        ProcessRuntime runtime,
        long maximumQueuedPayloadBytes,
        int maximumQueuedBuffers,
        int maximumQueuedSegments
    )
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumQueuedPayloadBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumQueuedBuffers);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumQueuedSegments);
        this.runtime = runtime;
        this.maximumQueuedPayloadBytes = maximumQueuedPayloadBytes;
        this.maximumQueuedBuffers = maximumQueuedBuffers;
        this.maximumQueuedSegments = maximumQueuedSegments;
    }

    public HostDistributionHandle Attach(string channel, ProcessContext controller)
    {
        ArgumentException.ThrowIfNullOrEmpty(channel);
        ArgumentNullException.ThrowIfNull(controller);
        HostDistributionHandle handle;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            if (!ReferenceEquals(controller.Runtime, runtime))
                throw new InvalidOperationException(HostDistributionDiagnostics.Owner);
            RemoveCompleted();
            RequireLive(controller);
            if (entries.ContainsKey(channel) || controllers.ContainsKey(controller.Self))
                throw new InvalidOperationException(HostDistributionDiagnostics.Attached);
            var entry = new HostDistributionEntry(
                channel,
                checked(++generation),
                controller
            );
            entries.Add(channel, entry);
            controllers.Add(controller.Self, entry);
            handle = new(this, entry);
        }
        _ = controller.Done.Task.ContinueWith(
            _ => Close(handle),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );

        return handle;
    }

    public HostDistributionLease Acquire(HostDistributionHandle handle)
    {
        lock (gate)
            _ = Require(handle);

        return new(handle);
    }

    public HostDistributionSnapshot Snapshot(HostDistributionHandle handle)
    {
        lock (gate)
        {
            var entry = Require(handle);

            return new(
                entry.Generation,
                entry.Controller.Self,
                entry.InputHandler,
                entry.GetSize,
                entry.NotificationArmed,
                entry.InputPackets,
                entry.OutputPackets,
                entry.QueuedPayloadBytes,
                entry.Output.Count,
                entry.QueuedSegments
            );
        }
    }

    public bool SetGetSize(HostDistributionHandle handle, ProcessContext caller, bool value)
    {
        lock (gate)
        {
            var entry = RequireController(handle, caller);
            bool previous = entry.GetSize;
            entry.GetSize = value;

            return previous;
        }
    }

    public bool GetSize(HostDistributionHandle handle, ProcessContext caller)
    {
        lock (gate)
            return RequireController(handle, caller).GetSize;
    }

    public void SetInputHandler(HostDistributionHandle handle, ProcessContext caller, Pid handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (gate)
        {
            var entry = RequireController(handle, caller);
            if (!runtime.IsCurrentLocal(handler))
                throw new InvalidOperationException(HostDistributionDiagnostics.Identity);
            entry.InputHandler = handler;
        }
    }

    public void RecordAcceptedHostInput(HostDistributionHandle handle, ProcessContext caller)
    {
        lock (gate)
        {
            var entry = Require(handle);
            RequireCaller(caller);
            if (!caller.Self.Equals(entry.InputHandler))
                throw new InvalidOperationException(HostDistributionDiagnostics.InputHandler);
            entry.InputPackets = checked(entry.InputPackets + 1);
        }
    }

    public void ArmNotification(HostDistributionHandle handle, ProcessContext caller)
    {
        Pid? notify = null;
        lock (gate)
        {
            var entry = RequireController(handle, caller);
            if (!entry.NotificationArmed)
            {
                if (entry.Output.Count != 0)
                    notify = entry.Controller.Self;
                else
                    entry.NotificationArmed = true;
            }
        }
        Notify(notify);
    }

    public bool TryEnqueueFinalizedHostBuffer(HostDistributionHandle handle, HostDistributionBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        Pid? notify = null;
        lock (gate)
        {
            var entry = Require(handle);
            if (entry.Output.Count >= maximumQueuedBuffers ||
                buffer.PayloadBytes > maximumQueuedPayloadBytes - entry.QueuedPayloadBytes ||
                buffer.SegmentCount > maximumQueuedSegments - entry.QueuedSegments)
                return false;
            entry.Output.Enqueue(buffer);
            entry.QueuedPayloadBytes += buffer.PayloadBytes;
            entry.QueuedSegments += buffer.SegmentCount;
            if (entry.NotificationArmed)
            {
                entry.NotificationArmed = false;
                notify = entry.Controller.Self;
            }
        }
        Notify(notify);

        return true;
    }

    internal Task WaitForOutputCapacity(HostDistributionHandle handle, HostDistributionBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        lock (gate)
        {
            var entry = Require(handle);
            if (buffer.PayloadBytes > maximumQueuedPayloadBytes || buffer.SegmentCount > maximumQueuedSegments)
                throw new ArgumentException(HostDistributionDiagnostics.OutputCapacity, nameof(buffer));
            if (entry.Output.Count < maximumQueuedBuffers &&
                buffer.PayloadBytes <= maximumQueuedPayloadBytes - entry.QueuedPayloadBytes &&
                buffer.SegmentCount <= maximumQueuedSegments - entry.QueuedSegments)
                return Task.CompletedTask;

            return (entry.CapacityChanged ??= new(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        }
    }

    public HostDistributionBuffer? TakeFinalizedHostBuffer(
        HostDistributionHandle handle,
        ProcessContext caller,
        out bool getSize
    )
    {
        lock (gate)
        {
            var entry = RequireController(handle, caller);
            getSize = entry.GetSize;
            if (!entry.Output.TryPeek(out var buffer))
                return null;
            long count = checked(entry.OutputPackets + 1);
            _ = entry.Output.Dequeue();
            entry.QueuedPayloadBytes -= buffer.PayloadBytes;
            entry.QueuedSegments -= buffer.SegmentCount;
            entry.OutputPackets = count;
            var previous = entry.CapacityChanged;
            entry.CapacityChanged = null;
            previous?.TrySetResult();

            return buffer;
        }
    }

    public void Close(HostDistributionHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        lock (gate)
        {
            if (!ReferenceEquals(handle.Owner, this))
                throw new InvalidOperationException(HostDistributionDiagnostics.ForeignHandle);
            Retire(handle.Entry);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            closed = true;
            foreach (var entry in entries.Values.ToArray())
                Retire(entry);
        }
    }

    private HostDistributionEntry Require(HostDistributionHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        if (!ReferenceEquals(handle.Owner, this))
            throw new InvalidOperationException(HostDistributionDiagnostics.ForeignHandle);
        var entry = handle.Entry;
        if (closed || entry.Closed || !entries.TryGetValue(entry.Channel, out var current) || !ReferenceEquals(current, entry))
            throw new InvalidOperationException(HostDistributionDiagnostics.Closed);
        RequireLive(entry.Controller);

        return entry;
    }

    private HostDistributionEntry RequireController(HostDistributionHandle handle, ProcessContext caller)
    {
        var entry = Require(handle);
        RequireCaller(caller);
        if (!ReferenceEquals(entry.Controller, caller))
            throw new InvalidOperationException(HostDistributionDiagnostics.Controller);

        return entry;
    }

    private void RequireCaller(ProcessContext caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        if (!ReferenceEquals(caller.Runtime, runtime))
            throw new InvalidOperationException(HostDistributionDiagnostics.Owner);
        RequireLive(caller);
    }

    private void RequireLive(ProcessContext controller)
    {
        if (runtime.IsHaltRequested || controller.Stop.IsCancellationRequested || controller.Done.Task.IsCompleted || !runtime.IsAlive(controller.Self))
            throw new InvalidOperationException(HostDistributionDiagnostics.Closed);
    }

    private void RemoveCompleted()
    {
        foreach (var entry in entries.Values.ToArray())
            if (entry.Controller.Stop.IsCancellationRequested || entry.Controller.Done.Task.IsCompleted || !runtime.IsAlive(entry.Controller.Self))
                Retire(entry);
    }

    private void Retire(HostDistributionEntry entry)
    {
        if (entry.Closed)
            return;
        entry.Closed = true;
        entry.NotificationArmed = false;
        entry.CapacityChanged?.TrySetCanceled();
        entry.CapacityChanged = null;
        entry.Output.Clear();
        entry.QueuedPayloadBytes = 0;
        entry.QueuedSegments = 0;
        if (entries.TryGetValue(entry.Channel, out var current) && ReferenceEquals(current, entry))
            entries.Remove(entry.Channel);
        if (controllers.TryGetValue(entry.Controller.Self, out current) && ReferenceEquals(current, entry))
            controllers.Remove(entry.Controller.Self);
    }

    private void Notify(Pid? controller)
    {
        if (controller is not null)
            runtime.SendSystem(controller, HostDistributionAtoms.Data);
    }
}
