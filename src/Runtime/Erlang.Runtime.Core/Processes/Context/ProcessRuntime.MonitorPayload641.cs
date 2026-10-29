namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeProcessPayload641 PrepareMonitorInput641(ProcessContext observer, Term target, ProcessMonitorOptions options)
        => RuntimeProcessPayload641.Prepare641(this, observer, Term.Tuple(target, options.Tag));

    private static ProcessMonitorOptions PreparedMonitorOptions641(ProcessMonitorOptions options, RuntimeProcessPayload641 input)
        => options with { Tag = ((TupleTerm)input.Value641).Items[1], Custody641 = input };

    internal ReferenceTerm MonitorSource(
        ProcessContext observer,
        string kind,
        Term target,
        ProcessMonitorOptions options
    )
    {
        if (options.Custody641 is not null)
            return MonitorSourceCore641(
                observer,
                kind,
                target,
                options
            );
        var input = PrepareMonitorInput641(observer, target, options);
        try
        {
            return MonitorSourceCore641(
                observer,
                kind,
                ((TupleTerm)input.Value641).Items[0],
                PreparedMonitorOptions641(options, input)
            );
        }
        finally
        {
            input.Close641();
        }
    }

    private ProcessMonitorEntry CreateMonitorEntry641(
        ProcessContext observer,
        Pid? target,
        Term reported,
        ProcessMonitorOptions options,
        ProcessMonitorSource source = ProcessMonitorSource.Process,
        PortTerm? port = null
    )
    {
        RuntimeProcessPayload641? custody = options.Custody641?.Borrow641(observer);
        try
        {
            return new(
                observer.Self,
                target,
                reported,
                options.Tag,
                options.Priority,
                options.AliasMode == ProcessAliasNames.Explicit,
                source,
                port,
                custody
            );
        }
        catch
        {
            custody?.Close641();
            throw;
        }
    }

    private void AddMonitor641(ReferenceTerm reference, ProcessMonitorEntry entry)
    {
        try
        {
            monitors.Add(reference, entry);
        }
        catch
        {
            entry.Custody641?.Close641();
            throw;
        }
    }

    private bool RemoveMonitor641(ReferenceTerm reference)
    {
        if (!monitors.Remove(reference, out var removed))
            return false;
        removed.Custody641?.Close641();

        return true;
    }

    private void RetireObserverMonitors641(ProcessContext observer)
    {
        foreach (var monitor in monitors.Where(pair => pair.Value.Observer.Equals(observer.Self)).ToArray())
            RemoveMonitor641(monitor.Key);
    }

    private void ClearMonitors641()
    {
        foreach (var entry in monitors.Values)
            entry.Custody641?.Close641();
        monitors.Clear();
    }

    internal void SendMonitorPayload641(
        ProcessContext observer,
        Term completeMessage,
        Func<bool?>? admission,
        RuntimeProcessPayload641? first,
        RuntimeProcessPayload641? second = null,
        SequenceTraceToken? token = null,
        bool preserve = true,
        Term? from = null
    )
    {
        lock (gate)
        {
            // Existing terminal signals remain observable during graceful VM
            // shutdown; fresh node/process admission is still closed.
            if (!ReferenceEquals(observer.Runtime, this) || IsHaltRequested || observer.Terminated
                || observer.Stop.IsCancellationRequested || observer.Done.Task.IsCompleted
                || !processes.TryGetValue(observer.Self, out var owned) || !ReferenceEquals(owned, observer))
                return;

            var publication = RuntimeProcessPayload641.CombinePublication641(
                this,
                observer,
                completeMessage,
                first,
                second
            );
            try
            {
                if (admission is not null)
                    observer.Mailbox.SendPendingSnapshot593(
                        publication.Value641,
                        admission,
                        token,
                        preserve,
                        from,
                        publication593: publication.Publication641
                    );
                else
                    observer.Mailbox.SendTracedSnapshot593(
                        publication.Value641,
                        token,
                        preserve,
                        from: from,
                        publication593: publication.Publication641
                    );
            }
            finally
            {
                publication.Close641();
            }

        }
    }
}
