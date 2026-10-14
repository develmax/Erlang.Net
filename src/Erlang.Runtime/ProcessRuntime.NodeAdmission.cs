namespace Erlang;

public sealed partial class ProcessRuntime
{
    private ProcessContext? localNodeKernel;
    private RuntimeLocalNodeEvents? localNodeEvents;

    public bool IsDistributionAlive
    {
        get
        {
            lock (gate)
                return localNodeKernel is { } kernel && kernel.DistributionRole == RuntimeDistributionRole.Kernel && IsOwnedLiveNodeProcess(kernel);
        }
    }

    public RuntimeLocalNodeEvents LocalNodeEvents
    {
        get
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, RuntimeLocalNodeAdmissionNames.Closed);

                return localNodeEvents ??= new(this, gate);
            }
        }
    }

    internal bool IsOwnedLiveNodeProcess(ProcessContext process)
    {
        return ReferenceEquals(process.Runtime, this) && !disposed && !IsHaltRequested && !process.Terminated &&
            !process.Stop.IsCancellationRequested && !process.Done.Task.IsCompleted &&
            processes.TryGetValue(process.Self, out var owned) && ReferenceEquals(owned, process);
    }

    internal bool IsCurrentLocal(Pid pid)
    {
        var current = LocalIdentity.Snapshot;
        var target = pid.WireIdentity;

        return pid.LocalIdentity is not null ? ReferenceEquals(pid.LocalIdentity, LocalIdentity) : target.Node == current.Node && target.Creation == current.Creation;
    }

    internal bool IsCurrentLocal(ReferenceTerm reference)
    {
        var current = LocalIdentity.Snapshot;
        var target = reference.WireIdentity;

        return reference.LocalIdentity is not null ? ReferenceEquals(reference.LocalIdentity, LocalIdentity) : target.Node == current.Node && target.Creation == current.Creation;
    }

    internal Term AdmitObservedLocalNode(ProcessContext caller, Term name, Term creation)
    {
        if (name is not Atom node || !DistributionNodeIdentity.IsValid(node) || node.Name == ProcessNodeDefaults.Local ||
            !RuntimeNodeCreation.TryConvert(creation, out uint requestedCreation) ||
            !Modules.HasExport(ErlangModuleNames.Module, RuntimeNodeControlNames.MonitorTrap, RuntimeLocalNodeAdmissionNames.MonitorTrapArity))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        lock (gate)
        {
            if (!IsOwnedLiveNodeProcess(caller) || localNodeKernel is not null ||
                !names.TryGetValue(RuntimeNodeControlNames.Kernel, out var registered) || registered is not Pid kernelPid || !IsCurrentLocal(kernelPid) ||
                !processes.TryGetValue(kernelPid, out var kernel) || !IsOwnedLiveNodeProcess(kernel) || kernel.DistributionRole != RuntimeDistributionRole.None)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            uint selected = SelectObservedExternalEpochCreation(node.Name, requestedCreation);
            kernel.DistributionRole = RuntimeDistributionRole.Kernel;
            localNodeKernel = kernel;
            LocalIdentity.Publish(node.Name, selected);
            localNodeEvents?.Notify(RuntimeLocalNodeAdmissionNames.NodeUp, node);

            return Term.A(ErlangBooleanAtoms.True);
        }
    }

    private void LocalNodeProcessTerminated(ProcessContext process, Term reason)
    {
        localNodeEvents?.ProcessTerminated(process);
        if (ReferenceEquals(localNodeKernel, process))
            StopLocalNode(reason);
    }

    private void StopLocalNode()
    {
        StopLocalNode(Term.A(RuntimeNodeMonitorNames.KernelTerminated));
    }

    private void StopLocalNode(Term reason)
    {
        if (localNodeKernel is null)
            return;
        Atom previous = NodeAtom;
        localNodeKernel.DistributionRole = RuntimeDistributionRole.None;
        localNodeKernel = null;
        distributionPending?.LocalKernelStopped(reason);
        LocalIdentity.Publish(ProcessNodeDefaults.Local, 0);
        localNodeEvents?.NotifyLocalDown(previous, reason);
    }
}
