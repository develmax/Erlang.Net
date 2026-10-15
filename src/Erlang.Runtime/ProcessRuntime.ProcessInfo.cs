namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> QueryProcessInfo(
        ProcessContext requester,
        Pid target,
        IReadOnlyList<ProcessInfoItem> keys,
        bool singleton
    )
    {
        requester.Cancellation.ThrowIfCancellationRequested();
        if (target.Equals(requester.Self) || !keys.Any(key => key.RequiresOwnerSnapshot))
            return ProcessInfo(target, keys, singleton);
        var request = new ProcessInfoRequest(
            requester,
            MakeReference(),
            keys,
            singleton
        );
        lock (gate)
        {
            if (!processes.TryGetValue(target, out var process))
                return ProcessInfoAtoms.Undefined;
            process.DeferredSignalDrain?.RequireSafeNestedWait();
            process.PendingInfoRequests.Add(request);
            process.Mailbox.SendPending(
                Nil.Value,
                () =>
            {
                lock (gate)
                {
                    if (process.PendingInfoRequests.Remove(request))
                        ReplyProcessInfo(request, ProcessInfo(target, request.Keys, request.Singleton), process.Self);
                }

                return null;
            }
            );
        }
        var reply = await requester.ReceiveAsync(message => message is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(request.Id) ? tuple : null);

        return reply!.Items[1];
    }

    private void ReplyProcessInfo(ProcessInfoRequest request, Term result, Term? from = null) => SendSystem(request.Requester.Self, Term.Tuple(request.Id, result), from);

    private Term SnapshotOriginMonitors(Pid observer) => Cons.From(monitors.Values.Where(monitor => monitor.Observer.Equals(observer)).Select(monitor => Term.Tuple(
        Term.A(monitor.Source switch
        {
            ProcessMonitorSource.Port => ProcessMonitorKinds.Port,
            ProcessMonitorSource.TimeOffset => ProcessMonitorKinds.TimeOffset,
            _ => ProcessMonitorKinds.Process
        }),
        monitor.ReportedTarget
    )));
}
