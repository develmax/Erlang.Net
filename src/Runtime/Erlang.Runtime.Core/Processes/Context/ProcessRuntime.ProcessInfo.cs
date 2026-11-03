using System.Runtime.ExceptionServices;

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
            return CaptureProcessInfoSnapshot601(target, keys, singleton).CompleteAndDispose(this, requester);
        var request = new ProcessInfoRequest(
            requester,
            MakeReference(),
            keys,
            singleton
        );
        ProcessContext? observedTarget601 = null;
        Exception? primary = null;
        try
        {
            lock (gate)
            {
                if (!processes.TryGetValue(target, out var process)) return ProcessInfoAtoms.Undefined;
                observedTarget601 = process;
                process.DeferredSignalDrain?.RequireSafeNestedWait();
                process.PendingInfoRequests.Add(request);
                if (keys.Any(key => key.Name == ProcessBinaryInfo823Names.Binary))
                    QueueBinaryInfo823(process, request);
                else
                {
                    process.Mailbox.SendPending(
                        Nil.Value,
                        new PendingProcessInfoAdmission817(
                            this,
                            process,
                            target,
                            request
                        ).Callback
                    );
                }
            }
            // Keep servicing this requester's control signals while waiting.
            // Two ordinary actors may query one another simultaneously.
            // Async Ready continuations still cannot run inline under target R/M.
            bool waiting601 = requester.WaitingForMessage;
            requester.WaitingForMessage = true;
            try
            {
                await requester.Mailbox.WaitForControlCompletion(request.Ready601.Task, requester.Cancellation);
            }
            finally
            {
                requester.WaitingForMessage = waiting601;
            }
            request.Snapshot601.RequireSuccess();

            return await requester.ReceiveProcessInfoReply601(request.Id);
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            // Closing/cancellation serializes with the whole target capture.
            lock (gate)
                // Logical removal precedes asynchronous storage retirement.
                // Always detach from the exact original context; membership in
                // the live process table is not its pending-request lifetime.
                observedTarget601?.PendingInfoRequests.Remove(request);
            try
            {
                ReleaseProcessInfoProducer601(request);
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }
    }

    // Called while R and possibly M are held. Only existing graph interests,
    // preallocated origin metadata and queue forks are touched here.
    private void PublishProcessInfoReply601(ProcessInfoRequest request, Term? from)
    {
        try
        {
            request.Snapshot601.RequireSuccess();
            request.Graph601 = MailboxRoots593.PrepareInterestGraph593(this, request.Snapshot601);
            request.SnapshotTransferred601 = true;
            request.Publication601 = MailboxPublicationScope593.Forward(this, request.Graph601);
            if (processes.TryGetValue(request.Requester.Self, out var receiver))
                receiver.Mailbox.SendTracedSnapshot593(
                    Term.Tuple(request.Id, request.Snapshot601.Value),
                    null,
                    preserve: true,
                    from: from,
                    publication593: request.Publication601
                );
        }
        catch (Exception error)
        {
            request.Snapshot601.Failure = error;
        }
        finally
        {
            request.Ready601.TrySetResult();
        }
    }

    // Existing termination calls are under R. Never settle producer roots here.
    private void ReplyProcessInfo(ProcessInfoRequest request, Term result, Term? from = null)
    {
        request.Snapshot601.Value = result;
        PublishProcessInfoReply601(request, from);
    }

    private static void ReleaseProcessInfoProducer601(ProcessInfoRequest request)
    {
        Exception? first = null, second = null, third = null;
        try
        {
            request.Publication601?.Dispose();
        }
        catch (Exception error)
        {
            first = error;
        }
        try
        {
            request.Graph601?.DisposeAsync().GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            second = error;
        }
        try
        {
            if (!request.SnapshotTransferred601) request.Snapshot601.Dispose();
        }
        catch (Exception error)
        {
            third = error;
        }
        if (first is not null || second is not null || third is not null)
            throw new AggregateException(new[] { first, second, third }.OfType<Exception>());
    }

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
