namespace Erlang;

public sealed partial class HostDistribution
{
    internal HostDistributionStagedOutput? TryStageOutput(HostDistributionHandle handle, HostDistributionBuffer packet)
    {
        lock (gate)
        {
            var entry = Require(handle);
            // Staging is a separate bounded budget; it cannot take capacity from an earlier normal ticket.
            if (entry.StagedOutput.Count >= maximumQueuedBuffers ||
                packet.PayloadBytes > maximumQueuedPayloadBytes - entry.StagedPayloadBytes ||
                packet.SegmentCount > maximumQueuedSegments - entry.StagedSegments)
                return null;
            long bytes = checked(entry.StagedPayloadBytes + packet.PayloadBytes);
            int segments = checked(entry.StagedSegments + packet.SegmentCount);
            long accounted = checked(entry.StagedAccountedBytes + packet.AccountedBytes);
            var staged = new HostDistributionStagedOutput(this, handle, packet);
            entry.StagedOutput.EnsureCapacity(checked(entry.StagedOutput.Count + 1));
            entry.StagedOutput.Add(staged);
            entry.StagedPayloadBytes = bytes;
            entry.StagedAccountedBytes = accounted;
            entry.StagedSegments = segments;

            return staged;
        }
    }

    internal bool TryEnqueueStagedOutput(HostDistributionStagedOutput staged)
    {
        Pid? notify = null;
        lock (gate)
        {
            if (!ReferenceEquals(staged.Owner, this))
                throw new InvalidOperationException(HostDistributionDiagnostics.ForeignHandle);
            var entry = Require(staged.Handle);
            if (!staged.Active || !entry.StagedOutput.Contains(staged))
                throw new InvalidOperationException(HostDistributionDiagnostics.Closed);
            var packet = staged.Packet;
            if (entry.Output.Count >= maximumQueuedBuffers ||
                packet.PayloadBytes > maximumQueuedPayloadBytes - entry.QueuedPayloadBytes ||
                packet.SegmentCount > maximumQueuedSegments - entry.QueuedSegments)
                return false;
            long queuedBytes = checked(entry.QueuedPayloadBytes + packet.PayloadBytes);
            long accounted = checked(entry.QueuedAccountedBytes + packet.AccountedBytes);
            int queuedSegments = checked(entry.QueuedSegments + packet.SegmentCount);
            long stagedBytes = checked(entry.StagedPayloadBytes - packet.PayloadBytes);
            int stagedSegments = checked(entry.StagedSegments - packet.SegmentCount);
            long stagedAccounted = checked(entry.StagedAccountedBytes - packet.AccountedBytes);
            entry.Output.EnsureCapacity(checked(entry.Output.Count + 1));
            entry.Output.Enqueue(packet);
            entry.StagedOutput.Remove(staged);
            staged.Active = false;
            entry.QueuedPayloadBytes = queuedBytes;
            entry.QueuedAccountedBytes = accounted;
            entry.QueuedSegments = queuedSegments;
            entry.NativePort457?.Wake();
            entry.StagedPayloadBytes = stagedBytes;
            entry.StagedAccountedBytes = stagedAccounted;
            entry.StagedSegments = stagedSegments;
            if (entry.NotificationArmed)
            {
                entry.NotificationArmed = false;
                notify = entry.Controller?.Self;
            }
        }
        Notify(notify);

        return true;
    }

    internal void ReleaseStagedOutput(HostDistributionStagedOutput staged)
    {
        lock (gate)
        {
            if (!ReferenceEquals(staged.Owner, this))
                throw new InvalidOperationException(HostDistributionDiagnostics.ForeignHandle);
            if (!staged.Active)
                return;
            var entry = staged.Handle.Entry;
            if (entry.StagedOutput.Contains(staged))
            {
                long bytes = checked(entry.StagedPayloadBytes - staged.Packet.PayloadBytes);
                long accounted = checked(entry.StagedAccountedBytes - staged.Packet.AccountedBytes);
                int segments = checked(entry.StagedSegments - staged.Packet.SegmentCount);
                entry.StagedOutput.Remove(staged);
                entry.StagedPayloadBytes = bytes;
                entry.StagedAccountedBytes = accounted;
                entry.StagedSegments = segments;
            }
            staged.Active = false;
        }
    }
}
