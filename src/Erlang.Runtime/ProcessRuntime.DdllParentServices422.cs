namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RequireDriverParentActor422(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
                throw new PlatformNotSupportedException(DdllErrorNames416.Profile);
        }
    }

    internal bool DriverInternalReference422(ReferenceTerm reference)
    {
        lock (gate)
            return reference.LocalIdentity is { } identity
                ? ReferenceEquals(identity, LocalIdentity)
                : reference.WireIdentity == LocalIdentity.Snapshot;
    }

    internal bool DriverTargetAlive422(Pid target)
    {
        lock (gate)
        {
            if (!IsCurrentLocal(target)) return false;

            return processes.TryGetValue(target, out var process) && IsOwnedLiveNodeProcess(process);
        }
    }

    internal bool DriverTargetInternal422(Pid target)
    {
        lock (gate)
            return ReferenceEquals(target.LocalIdentity, LocalIdentity)
                || target.LocalIdentity is null && target.WireIdentity == LocalIdentity.Snapshot;
    }

    internal void RemoveDriverConnectedLink422(Pid target, PortTerm port)
    {
        lock (gate)
        {
            if (!IsCurrentLocal(target)) return;
            if (processes.TryGetValue(target, out var process))
            {
                process.PortLinks.Remove(port);
                process.PriorityPortLinks.Remove(port);
            }
        }
    }
}
