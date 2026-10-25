namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal bool TryPublishImmediateControl(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        uint connectionId,
        ProcessContext producer,
        TupleTerm control,
        Func<bool> stateValid,
        Action commit
    )
    {
        LocalNodeSnapshot snapshot;
        DistributionOutputTicket ticket;
        lock (gate)
        {
            RequireProcess(producer);
            if (!IsConnectedSignalGeneration(entry, connected, connectionId) || !stateValid())
                return false;
            snapshot = runtime.LocalIdentity.Snapshot;
            ticket = ReserveOwnedOutput(connected, producer, immediate: true);
        }
        try
        {
            byte[] bytes = DistributionPassThroughCodec.EncodeProcessControl(
                control,
                negotiatedFlags: connected.Flags,
                options: new EtfEncodingOptions
                {
                    NodeIdentity = runtime.LocalIdentity,
                    NodeSnapshot = snapshot,
                    FunctionOwner = producer.Self
                }
            );
            var packet = new HostDistributionBuffer([bytes.AsMemory()]);
            lock (gate)
            {
                RequireProcess(producer);
                if (!IsConnectedSignalGeneration(entry, connected, connectionId) || !stateValid())
                    return false;
                bool admitted = connected.Queue.TryEnqueueFinalizedHostBuffer(connected.QueueHandle, packet);
                if (!admitted)
                    throw new DistributionOutputWouldBlockException();
                commit();

                return true;
            }
        }
        finally
        {
            ReleaseOwnedOutput(connected, ticket);
        }
    }
}
