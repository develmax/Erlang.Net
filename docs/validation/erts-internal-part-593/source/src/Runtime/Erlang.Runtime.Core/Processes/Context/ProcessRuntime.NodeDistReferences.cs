namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> ReadNodeDistReferences(ProcessContext context, Term query)
    {
        bool trap = query is Atom;
        lock (gate)
        {
            RequireDebugInternalEnabled419(context);
            if (!trap && (query is not TupleTerm { Items.Count: 2 } tuple
                || tuple.Items[1] is not Atom { Name: RuntimeMultiSchedulingNames.Blocked }
                || !multiSchedulingBlockers.Values.Any(owner => owner.HasAllBlock)))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        if (trap)
            return await Modules.Call(
                context,
                ErtsInternalContract.Module,
                ErtsInternalFunctions.GetInternalStateBlocked,
                query
            );
        // The native OTP implementation stops thread progress for this scan.
        // Acquire the genuine actor barrier, not just the runtime monitor.
        using (await actorDispatcher.AcquireNormalBarrier696(context))
        {
            var snapshot = new NodeDistReferenceSnapshot();
            ProcessContext[] contexts;
            LocalNodeSnapshot local;
            (Pid Owner, Term Root)[] linkRoots;
            (Pid Owner, Term Root)[] monitorRoots;
            (Pid Owner, Term Root)[] localMonitorRoots;
            lock (gate)
            {
                RequireDebugInternalEnabled419(context);
                if (!multiSchedulingBlockers.Values.Any(owner => owner.HasAllBlock)) throw new ErlangException(ErlangErrorReasons.BadArgument);
                local = LocalIdentity.Snapshot;
                contexts = processes.Values.Concat(executingProcesses).Distinct().ToArray();
                distributionPending?.CaptureNodeDistReferences(snapshot);
                localMonitorRoots = monitors.Values.Select(monitor => (monitor.Observer, monitor.Tag)).ToArray();
                linkRoots = distributionProcessLinks.Select(link => (link.Local.Self, (Term)link.Remote)).ToArray();
                monitorRoots = distributionProcessMonitors.Select(monitor => (monitor.Local.Self,
                    (Term)Term.Tuple(monitor.Remote, monitor.Watched, monitor.Reference))).ToArray();

            }
            // No heap monitor or identifier read is nested under the runtime
            // gate. Storage owners pin their physical payloads while scanning.
            foreach (var link in linkRoots)
                snapshot.Graph(link.Root, NodeDistReferenceSnapshot.Owner(NodeDistReferenceNames.Process, link.Owner), NodeDistReferenceNames.Link);
            foreach (var monitor in monitorRoots)
                snapshot.Graph(
                    monitor.Root,
                    NodeDistReferenceSnapshot.Owner(NodeDistReferenceNames.Process, monitor.Owner),
                    NodeDistReferenceNames.Monitor
                );
            foreach (var monitor in localMonitorRoots)
                snapshot.Graph(
                    monitor.Root,
                    NodeDistReferenceSnapshot.Owner(NodeDistReferenceNames.Process, monitor.Owner),
                    NodeDistReferenceNames.Monitor
                );
            ProcessTracing.CaptureNodeDistReferences(snapshot);
            foreach (var process in contexts) process.CaptureNodeDistReferences(snapshot);
            foreach (TupleTerm port in Ports.CaptureNodeDistReferences())
                snapshot.Graph(port.Items[1], port.Items[0], NodeDistReferenceNames.Heap);
            foreach (TupleTerm port in driverFrontend422?.CaptureNodeDistReferences() ?? [])
                snapshot.Graph(port.Items[1], port.Items[0], NodeDistReferenceNames.Heap);
            etsFoundation?.CaptureNodeDistReferences(snapshot);
            persistentTerms?.CaptureNodeDistReferences(snapshot);
            Timers.CaptureNodeDistReferences(snapshot);

            return snapshot.Complete(local);
        }
    }
}
