namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void QueueConnectedDeferredExit(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        Pid remote,
        Pid local,
        ReadOnlyMemory<byte> payload,
        Term traceToken,
        bool explicitSignal,
        Action<Exception> malformedReason
    )
    {
        ProcessContext target;
        DistributionProcessLink? link;
        lock (gate)
        {
            if (!IsCurrentLocal(local) || !processes.TryGetValue(local, out target!))
                return;
            link = explicitSignal ? null : distributionProcessLinks.LastOrDefault(item =>
                ReferenceEquals(item.Entry, entry) && ReferenceEquals(item.Connection, connected) &&
                item.ConnectionId == entry.ConnectionId && ReferenceEquals(item.Local, target) && item.Remote.Equals(remote));
            if (!explicitSignal && link is null)
                return;
        }
        var reason = CreateDeferredDistributionExitReason(payload, () => target.Mailbox.DeferredGateHeld);
        bool priority = false;

        bool Eligible()
        {
            lock (gate)
            {
                if (!IsOwnedLiveNodeProcess(target))
                    return false;
                if (link is null)
                    return true;
                distributionProcessLinks.Remove(link);
                if (!target.LinkIdentities.TryGetValue(remote, out var current) || !ReferenceEquals(current, link.Identity) ||
                    current.PendingUnlink is not null || !target.Links.Contains(remote))
                    return false;
                priority = target.PriorityLinks.Contains(remote);
                RemoveDistributionLinkIdentity(link);

                return true;
            }
        }

        DeferredMailboxAdmission? Prepare()
        {
            EtfDecodedTerm? decoded;
            try
            {
                decoded = reason.Prepare(Eligible, malformedReason).GetAwaiter().GetResult();
            }
            catch (Exception exception) when (DistributionDeferredExitReason.IsMalformed(exception))
            {
                return null;
            }
            if (decoded is null)
                return null;
            Term value = decoded.Term;
            lock (gate)
            {
                if (!IsOwnedLiveNodeProcess(target))
                    return null;
                bool kill = explicitSignal && value.Equals(Term.A(ProcessExitSignals.Kill));
                if (target.TrapExits && !kill)
                    return new(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), remote, value), priority);
                if (!kill && value.Equals(Term.A(ProcessExitReasons.Normal)))
                    return null;
                Terminate(target, kill ? Term.A(ProcessExitReasons.Killed) : value);

                return null;
            }
        }
        QueueDeferredProcessSignal(
            target,
            Prepare,
            reason.Dispose,
            remote,
            traceToken
        );
    }
}
