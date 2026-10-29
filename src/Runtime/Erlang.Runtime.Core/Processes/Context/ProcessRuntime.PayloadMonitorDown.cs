namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void QueueConnectedDeferredMonitorDown(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        Term watched,
        Pid watcher,
        ReferenceTerm reference,
        ReadOnlyMemory<byte> payload,
        Action<Exception> malformedReason
    )
    {
        ProcessContext observer;
        uint connectionId = entry.ConnectionId;
        lock (gate)
        {
            // OTP dist.c drops dead/current-node stale watchers before decoding.
            if (!IsCurrentLocal(watcher) || !processes.TryGetValue(watcher, out observer!))
                return;
        }
        var reason = CreateDeferredDistributionExitReason(payload, () => observer.Mailbox.DeferredGateHeld);

        bool Eligible()
        {
            lock (gate)
                return IsOwnedLiveNodeProcess(observer);
        }

        DeferredMailboxAdmission? Prepare()
        {
            EtfDecodedTerm? decoded;
            try
            {
                // OTP GEN_EXIT decodes BEFORE monitor-tree lookup, including unknown refs.
                decoded = reason.Prepare(Eligible, malformedReason).GetAwaiter().GetResult();
            }
            catch (Exception exception) when (DistributionDeferredExitReason.IsMalformed(exception))
            {
                return null;
            }
            if (decoded is null)
                return null;
            RuntimeProcessPayload641 decoded641;
            try
            {
                decoded641 = RuntimeProcessPayload641.Prepare641(this, observer, decoded.Term);
            }
            catch (ObjectDisposedException)
            {
                lock (gate)
                    if (!IsOwnedLiveNodeProcess(observer))
                        return null;
                throw;
            }
            RuntimeProcessPayload641? publication641 = null;
            try
            {
                lock (gate)
                {
                    if (!IsOwnedLiveNodeProcess(observer))
                        return null;
                    var monitor = distributionProcessMonitors.FirstOrDefault(item => item.Origin &&
                        ReferenceEquals(item.Entry, entry) && ReferenceEquals(item.Connection, connected) &&
                        item.ConnectionId == connectionId && ReferenceEquals(item.Local, observer) && item.Reference.Equals(reference));
                    if (monitor is null)
                        return null;
                    Term reported = watched is Atom name ? Term.Tuple(name, Term.A(entry.Node.Name)) : watched;
                    ProcessMonitorEntry? localOptions = monitor.SpawnOptions ? monitors.GetValueOrDefault(reference) : null;
                    if (monitor.SpawnOptions && localOptions is null)
                        return null;
                    Term message = Term.Tuple(
                        localOptions?.Tag ?? Term.A(ProcessMessageTags.MonitorDown),
                        reference,
                        Term.A(ProcessMonitorKinds.Process),
                        reported,
                        decoded641.Value641
                    );
                    publication641 = RuntimeProcessPayload641.CombinePublication641(
                        this,
                        observer,
                        message,
                        localOptions?.Custody641,
                        decoded641
                    );
                    distributionProcessMonitors.Remove(monitor);
                    if (localOptions is not null)
                    {
                        RemoveMonitor641(reference);
                        if (!localOptions.KeepAlias) aliases.Remove(reference);
                    }
                    var admitted = new DeferredMailboxAdmission(publication641.Value641, localOptions?.Priority ?? false)
                    { Payload641 = publication641 };
                    publication641 = null;

                    return admitted;
                }
            }
            finally
            {
                publication641?.Close641();
                decoded641.Close641();
            }
        }
        QueueDeferredProcessSignal(
            observer,
            Prepare,
            reason.Dispose,
            entry.Node,
            Nil.Value
        );
    }
}
