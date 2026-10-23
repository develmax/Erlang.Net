namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal ProcessContext? ReadNativeDistributionKernel429(bool requireRegistered = true)
    {
        lock (gate) return localNodeKernel is { } kernel && kernel.DistributionRole == RuntimeDistributionRole.Kernel
         && IsOwnedLiveNodeProcess(kernel) && (!requireRegistered || WhereIs(RuntimeNodeControlNames.Kernel).Equals(kernel.Self)) ? kernel : null;
    }

    internal ProcessContext? ReadNativeDistributionController429(Pid pid)
    {
        lock (gate) return IsCurrentLocal(pid) && processes.TryGetValue(pid, out var controller) && IsOwnedLiveNodeProcess(controller) ? controller : null;
    }

    internal Term CreateDistChannel429(
        ProcessContext caller,
        Term node,
        Term controller,
        Term options
    )
    {
        lock (gate)
        {
            RequireBeamfileActor418(caller);
            // Pinned native checks the genuine registered local kernel before arguments.
            ProcessContext? kernel = ReadNativeDistributionKernel429();
            if (kernel is null) return Term.A(DistChannel429Names.BadArgument);
            if (node is not Atom remote || !DistributionNodeIdentity.IsValid(remote) || remote.Name == Node
             || controller is not (Pid or PortTerm) || options is not TupleTerm { Items.Count: 2 } tuple
             || tuple.Items[0] is not Integer flags || flags.Value < 0 || flags.Value > ulong.MaxValue
             || tuple.Items[1] is not Integer creation || creation.Value < 0 || creation.Value > uint.MaxValue
             || !DistributionCapabilityProfile429.HasMandatory((ulong)flags.Value)) return Term.A(DistChannel429Names.BadArgument);
            if (DistributionCapabilityProfile429.HasUnsupported((ulong)flags.Value)) throw new NotSupportedException(DistChannel429Names.FlagsFrontier);
            if (controller is PortTerm port) return distributionPending?.CreateNativePortChannel457(
                caller,
                remote,
                port,
                kernel,
                (ulong)flags.Value,
                (uint)creation.Value
            )
                ?? Term.A(DistChannel429Names.BadArgument);
            var pid = (Pid)controller;
            if (pid.LocalIdentity is not null && !ReferenceEquals(pid.LocalIdentity, LocalIdentity)) throw new InvalidOperationException(DistChannel429Names.Foreign);
            ProcessContext? target = ReadNativeDistributionController429(pid);
            if (target is null || distributionPending is null) return Term.A(DistChannel429Names.BadArgument);

            return distributionPending.CreateNativePidChannel429(
                caller,
                remote,
                target,
                kernel,
                (ulong)flags.Value,
                (uint)creation.Value
            );
        }
    }
}
