namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> DisplayProcess(ProcessContext requester, Pid target)
    {
        requester.Cancellation.ThrowIfCancellationRequested();
        ProcessDisplayRequest request;
        lock (gate)
        {
            if (target.Equals(requester.Self))
                return processes.TryGetValue(target, out var self) ? DisplayOwnedProcess(self) : Term.A(ErlangErrorReasons.BadArgument);
            request = new ProcessDisplayRequest(requester, MakeReference(), target);
            if (!processes.TryGetValue(target, out var process))
                return Term.A(ErlangErrorReasons.BadArgument);
            process.PendingDisplayRequests.Add(request);
            process.Mailbox.SendPending(
                Nil.Value,
                () =>
            {
                lock (gate)
                {
                    if (process.PendingDisplayRequests.Contains(request))
                    {
                        Term result = DisplayOwnedProcess(process);
                        process.PendingDisplayRequests.Remove(request);
                        ReplyProcessDisplay(request, result);
                    }
                }

                return null;
            }
            );
        }
        var reply = await requester.ReceiveAsync(message => message is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(request.Id) ? tuple : null);

        return reply!.Items[1];
    }

    private Term DisplayOwnedProcess(ProcessContext process)
    {
        if (process.Terminated)
            return Term.A(ErlangErrorReasons.BadArgument);
        string text = ProcessDisplayText.Format(process.Self, process.SnapshotDisplayFrames());
        StandardStreams.Write(ErlangStandardStreamNames.StandardError, Term.String(text));

        return Term.A(ErlangBooleanAtoms.True);
    }

    private void ReplyProcessDisplay(ProcessDisplayRequest request, Term result) => SendSystem(request.Requester.Self, Term.Tuple(request.Id, result), request.Target);
}
