namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly Dictionary<ProcessContext, HashSet<ProcessAliveRequest425>> processAliveRequests425 = new();

    internal Term RequestProcessAlive425(ProcessContext requester, IReadOnlyList<Term> arguments)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(requester);
            requester.Cancellation.ThrowIfCancellationRequested();
            if (arguments[0] is not Pid target || !IsCurrentLocal(target)
                || arguments[1] is not ReferenceTerm reference || reference is ResourceReferenceTerm || !IsCurrentLocal(reference))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var request = new ProcessAliveRequest425(requester.Self, reference);
            ProcessContext? process = FindProcessAliveTarget425(target);
            if (process is null)
            {
                ReplyProcessAlive425(request, false);
                // The pinned failed-send return handles already queued incoming signals.
                requester.Mailbox.AdmitPending();

                return Term.A(ProcessAliveNames425.Ok);
            }
            QueueProcessAlive425(process, request);

            return Term.A(ProcessAliveNames425.Ok);
        }
    }

    private ProcessContext? FindProcessAliveTarget425(Pid pid)
    {
        if (processes.TryGetValue(pid, out var process))
            return process;
        foreach (var exit in exitContinuations.Values)
            if (!exit.LocalCleanup.Task.IsCompleted && exit.Process.Self.Equals(pid))
                return exit.Process;

        return null;
    }

    private void QueueProcessAlive425(ProcessContext process, ProcessAliveRequest425 request)
    {
        if (!processAliveRequests425.TryGetValue(process, out var requests))
        {
            requests = new();
            processAliveRequests425.Add(process, requests);
        }
        requests.Add(request);
        if (process.Terminated)
            return;
        try
        {
            process.Mailbox.SendPending(
                Nil.Value,
                () =>
            {
                lock (gate)
                {
                    if (requests.Remove(request))
                    {
                        if (requests.Count == 0) processAliveRequests425.Remove(process);
                        ReplyProcessAlive425(request, !process.Terminated);
                    }
                }

                return null;
            },
                from: request.Requester,
                traceAdmission: false
            );
        }
        catch
        {
            requests.Remove(request);
            if (requests.Count == 0) processAliveRequests425.Remove(process);
            throw;
        }
    }

    // Called under gate from FinishTermination, before DOWN/link notifications.
    private void FinishProcessAlive425(ProcessContext target)
    {
        if (!processAliveRequests425.Remove(target, out var requests)) return;
        List<Exception>? failures = null;
        foreach (var request in requests)
        {
            try
            {
                ReplyProcessAlive425(request, false);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }

    private void ReplyProcessAlive425(ProcessAliveRequest425 request, bool result)
    {
        if (request.Completion is { } completion)
            completion.TrySetResult(result);
        else
            SendSystem(request.Requester, Term.Tuple(request.Reference!,
                Term.A(result ? ProcessAliveNames425.True : ProcessAliveNames425.False)));
    }

    // Halt discards all pending signals and guest replies, matching existing request classes.
    private void CloseProcessAlive425() => processAliveRequests425.Clear();
}
