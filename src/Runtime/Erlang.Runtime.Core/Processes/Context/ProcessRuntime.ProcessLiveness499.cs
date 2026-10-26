namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> QueryProcessAlive499(ProcessContext requester, Term argument)
    {
        ProcessContext? target = null;
        ProcessAliveRequest425? request = null;
        TaskCompletionSource<bool>? completion = null;
        bool alive;
        lock (gate)
        {
            if (argument is not Pid pid || pid.Node != Node)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (pid.Equals(requester.Self))
                return CoreModules.Boolean(true);
            alive = processes.TryGetValue(pid, out target);
            if (alive && (target!.Terminated || target.Mailbox.HasPendingSignals428))
            {
                completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                request = new(requester.Self, null, completion);
                QueueProcessAlive425(target, request);
            }
        }
        if (completion is not null)
        {
            try
            {
                await requester.Mailbox.WaitForControlCompletion(completion.Task, requester.Cancellation);
                alive = await completion.Task;
            }
            finally
            {
                lock (gate)
                    if (processAliveRequests425.TryGetValue(target!, out var requests) && requests.Remove(request!) && requests.Count == 0)
                        processAliveRequests425.Remove(target!);
            }
        }
        AdmitSignals(requester);

        return CoreModules.Boolean(alive);
    }
}
