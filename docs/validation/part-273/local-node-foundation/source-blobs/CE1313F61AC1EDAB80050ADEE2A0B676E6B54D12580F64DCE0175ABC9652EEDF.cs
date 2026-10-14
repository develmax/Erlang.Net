using System.Diagnostics;

namespace Erlang;

public sealed class RuntimeDistributionPending
{
    private readonly ProcessRuntime runtime;
    private readonly object gate;
    private readonly Dictionary<string, WeakReference<RuntimeDistributionPendingEntry>> entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RuntimeDistributionPendingEntry> pending = new(StringComparer.Ordinal);
    private readonly Dictionary<Pid, RuntimeDistributionPendingEntry> controllers = new();
    private ProcessContext? kernelSetup;
    private bool closed;

    internal RuntimeDistributionPending(ProcessRuntime runtime, object gate, int maximumNodes)
    {
        this.runtime = runtime;
        this.gate = gate;
        MaximumNodes = maximumNodes;
    }

    public int MaximumNodes { get; }

    public TupleTerm BeginPending(Atom remoteNode)
    {
        ArgumentNullException.ThrowIfNull(remoteNode);
        lock (gate)
        {
            RequireOpen();
            if (!DistributionNodeIdentity.IsValid(remoteNode) || remoteNode.Name == runtime.Node)
                throw new ArgumentException(DistributionPendingDiagnostics.Node, nameof(remoteNode));
            SweepIdle();
            RuntimeDistributionPendingEntry entry;
            if (entries.TryGetValue(remoteNode.Name, out var weak) && weak.TryGetTarget(out var existing))
                entry = existing;
            else
            {
                if (entries.Count >= MaximumNodes)
                    throw new InvalidOperationException(DistributionPendingDiagnostics.Capacity);
                entry = new(
                    this,
                    runtime.ReferenceResources,
                    remoteNode,
                    unchecked((uint)Stopwatch.GetTimestamp()) & DistributionHandleLayout.ConnectionIdMask
                );
                entry.Reference = runtime.ReferenceResources.CreateDistributionEntry(entry);
                entries.Add(remoteNode.Name, new(entry));
            }
            if (entry.State == DistributionPendingState.Idle)
            {
                entry.ConnectionId = (entry.ConnectionId + 1) & DistributionHandleLayout.ConnectionIdMask;
                entry.State = DistributionPendingState.Pending;
                pending.Add(entry.Node.Name, entry);
            }

            return Term.Tuple(Term.I(entry.ConnectionId), entry.Reference);
        }
    }

    public DistributionPendingLease Acquire(Term handle)
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            try
            {
                ValidateEntry(entry, connectionId);

                return new(resource, connectionId);
            }
            catch
            {
                resource.Dispose();
                throw;
            }
        }
    }

    public bool AbortPending(Term handle)
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                ValidateEntry(entry, connectionId);
                if (entry.State != DistributionPendingState.Pending)
                    return false;
                RetirePending(entry);

                return true;
            }
        }
    }

    public void ReserveKernelSetup(ProcessContext kernel)
    {
        lock (gate)
        {
            RequireProcess(kernel);
            if (!runtime.WhereIs(RuntimeNodeControlNames.Kernel).Equals(kernel.Self))
                throw new InvalidOperationException(DistributionPendingDiagnostics.Kernel);
            if (kernelSetup is not null && !ReferenceEquals(kernelSetup, kernel) || controllers.ContainsKey(kernel.Self))
                throw new InvalidOperationException(DistributionPendingDiagnostics.Controller);
            kernelSetup = kernel;
        }
    }

    public void ReleaseKernelSetup(ProcessContext kernel)
    {
        lock (gate)
        {
            RequireProcess(kernel);
            if (!ReferenceEquals(kernelSetup, kernel))
                throw new InvalidOperationException(DistributionPendingDiagnostics.Kernel);
            kernelSetup = null;
            foreach (var entry in pending.Values.ToArray())
                RetirePending(entry);
        }
    }

    public void ReserveControllerSetup(Term handle, ProcessContext controller)
    {
        lock (gate)
        {
            RequireProcess(controller);
            if (kernelSetup is null || !runtime.IsAlive(kernelSetup.Self) ||
                !runtime.WhereIs(RuntimeNodeControlNames.Kernel).Equals(kernelSetup.Self))
                throw new InvalidOperationException(DistributionPendingDiagnostics.Kernel);
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                ValidateEntry(entry, connectionId);
                if (entry.State != DistributionPendingState.Pending || entry.ReservedController is not null ||
                    ReferenceEquals(kernelSetup, controller) || controllers.ContainsKey(controller.Self))
                    throw new InvalidOperationException(DistributionPendingDiagnostics.Controller);
                entry.ReservedController = controller;
                controllers.Add(controller.Self, entry);
            }
        }
    }

    internal DistributionPendingSnapshot SnapshotEntry(RuntimeDistributionPendingEntry entry, uint connectionId)
    {
        lock (gate)
        {
            ValidateEntry(entry, connectionId);

            return new(
                entry.Node,
                entry.ConnectionId,
                entry.State,
                entry.ReservedController?.Self,
                kernelSetup?.Self
            );
        }
    }

    internal void ProcessTerminated(ProcessContext process)
    {
        lock (gate)
        {
            if (ReferenceEquals(kernelSetup, process))
            {
                kernelSetup = null;
                foreach (var entry in pending.Values.ToArray())
                    RetirePending(entry);
            }
            if (controllers.TryGetValue(process.Self, out var owned) && ReferenceEquals(owned.ReservedController, process))
                RetirePending(owned);
        }
    }

    internal void Close()
    {
        lock (gate)
        {
            if (closed)
                return;
            closed = true;
            foreach (var weak in entries.Values)
                if (weak.TryGetTarget(out var entry))
                {
                    entry.State = DistributionPendingState.Closed;
                    entry.ReservedController = null;
                }
            entries.Clear();
            pending.Clear();
            controllers.Clear();
            kernelSetup = null;
        }
    }

    private RuntimeDistributionPendingEntry ReadHandle(
        Term handle,
        out uint connectionId,
        out ReferenceResourceLease<RuntimeDistributionPendingEntry> lease
    )
    {
        RequireOpen();
        if (handle is not TupleTerm { Items.Count: DistributionHandleLayout.Arity } tuple || tuple.Items[DistributionHandleLayout.ConnectionIdIndex] is not Integer integer ||
            integer.Value < DistributionHandleLayout.NativeSmallMinimum || integer.Value > DistributionHandleLayout.NativeSmallMaximum ||
            tuple.Items[DistributionHandleLayout.ReferenceIndex] is not ReferenceTerm reference)
            throw new ArgumentException(DistributionPendingDiagnostics.Handle, nameof(handle));
        connectionId = unchecked((uint)(long)integer.Value);
        lease = runtime.ReferenceResources.AcquireDistributionEntry(reference)
            ?? throw new ArgumentException(DistributionPendingDiagnostics.Handle, nameof(handle));
        try
        {
            var entry = lease.Value;
            if (!ReferenceEquals(entry.Owner, this))
                throw new ArgumentException(DistributionPendingDiagnostics.Handle, nameof(handle));

            return entry;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    private void ValidateEntry(RuntimeDistributionPendingEntry entry, uint connectionId)
    {
        RequireOpen();
        if (!ReferenceEquals(entry.Owner, this) || entry.State == DistributionPendingState.Closed || entry.ConnectionId != connectionId)
            throw new ArgumentException(DistributionPendingDiagnostics.Handle);
    }

    private void RequireProcess(ProcessContext process)
    {
        ArgumentNullException.ThrowIfNull(process);
        RequireOpen();
        if (!ReferenceEquals(process.Runtime, runtime) || !runtime.IsAlive(process.Self) || process.Stop.IsCancellationRequested || process.Done.Task.IsCompleted)
            throw new InvalidOperationException(DistributionPendingDiagnostics.Process);
    }

    private void RequireOpen()
    {
        ObjectDisposedException.ThrowIf(closed || runtime.IsHaltRequested, DistributionPendingDiagnostics.Closed);
    }

    private void RetirePending(RuntimeDistributionPendingEntry entry)
    {
        if (entry.ReservedController is { } controller && controllers.TryGetValue(controller.Self, out var owned) && ReferenceEquals(owned, entry))
            controllers.Remove(controller.Self);
        entry.ReservedController = null;
        entry.State = DistributionPendingState.Idle;
        pending.Remove(entry.Node.Name);
    }

    private void SweepIdle()
    {
        foreach (var pair in entries.ToArray())
            if (!pair.Value.TryGetTarget(out _))
                entries.Remove(pair.Key);
    }
}
