namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask PurgeCodeImageProcesses(ProcessContext worker, ModuleCodeImage image)
    {
        ProcessContext[] candidates;
        lock (gate)
            candidates = processes.Values.Concat(executingProcesses).Distinct().ToArray();
        foreach (var candidate in candidates)
            if (!ReferenceEquals(candidate, worker))
                await PurgeCodeImageProcess(worker, candidate, image);
    }

    internal async ValueTask PurgeCodeImageProcess(ProcessContext worker, ProcessContext candidate, ModuleCodeImage image)
    {
        bool terminated;
        lock (gate)
            terminated = candidate.Terminated;
        if (terminated)
        {
            if (candidate.IsExecutingCode(image))
                await candidate.Unwound.Task.WaitAsync(worker.Cancellation);

            return;
        }
        if (!(await CheckProcessCodeImage(
            worker,
            candidate.Self,
            image,
            false,
            Nil.Value
        )).Equals(Term.A(ErlangBooleanAtoms.True)))
        {
            if (candidate.IsExecutingCode(image))
                await candidate.Unwound.Task.WaitAsync(worker.Cancellation);

            return;
        }
        var monitor = Monitor(worker, candidate.Self);
        Exit(worker, candidate.Self, Term.A(ProcessExitSignals.Kill));
        await worker.ReceiveAsync(message => message is TupleTerm { Items.Count: 5 } tuple &&
            tuple.Items[0] is Atom { Name: ProcessMessageTags.MonitorDown } && tuple.Items[1].Equals(monitor) ? tuple : null);
        await candidate.Unwound.Task.WaitAsync(worker.Cancellation);
    }
}
