namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    // io.c WAIT_BUSY and erl_port_task.c SIG_DEP permit independent callers'
    // non-output signals to pass busy output, preserving each actual sender's
    // order. From in a bang envelope is not this dependency identity.
    private Task<Term> EnqueueSignal454(
        NativeDriverPortRow422 row,
        Pid? caller,
        bool waitsForBusy,
        Func<Task<Term>> execute,
        Func<Task<Term>, Task<Term>>? reply = null
    )
    {
        if (row.SignalFailure is { } failure) throw new IOException(DriverParentWireNames422.Invalid, failure);
        var signal = new NativeDriverSignal454(caller, waitsForBusy, execute);
        Task<Term> result = reply?.Invoke(signal.Completion.Task) ?? signal.Completion.Task;
        Track(result);
        if (row.SignalResults.Count == 0)
        {
            row.SignalsDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
            row.Commands = row.SignalsDrained.Task;
        }
        row.SignalResults.Add(result);
        _ = result.ContinueWith(
            _ => RemoveSignalResult454(row, result),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
        row.Signals.AddLast(signal);
        row.SignalsChanged.TrySetResult();
        if (!row.SignalsRunning)
        {
            var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task runner = RunSignals454(row, accepted.Task);
            try
            {
                Track(runner);
            }
            catch
            {
                row.Signals.Remove(signal);
                signal.Completion.TrySetCanceled();
                accepted.TrySetCanceled();
                throw;
            }
            row.SignalsRunning = true;
            accepted.TrySetResult();
        }

        return result;
    }

    private void RemoveSignalResult454(NativeDriverPortRow422 row, Task result)
    {
        lock (gate)
        {
            row.SignalResults.Remove(result);
            if (result.Exception is { } failures) row.SignalFailures.AddRange(failures.Flatten().InnerExceptions);
            else if (result.IsCanceled) row.SignalFailures.Add(new TaskCanceledException(result));
            if (row.SignalResults.Count != 0) return;
            if (row.SignalFailures.Count == 0) row.SignalsDrained!.TrySetResult();
            else row.SignalsDrained!.TrySetException(row.SignalFailures.ToArray());
            row.SignalFailures.Clear();
        }
    }

    private async Task RunSignals454(NativeDriverPortRow422 row, Task accepted)
    {
        await accepted.ConfigureAwait(false);
        try
        {
            while (true)
            {
                NativeDriverSignal454? next = null;
                LinkedListNode<NativeDriverSignal454>? selected = null;
                Task? changed = null;
                lock (gate)
                {
                    if (row.Signals.Count == 0)
                    {
                        row.SignalsRunning = false;

                        return;
                    }
                    var blocked = new HashSet<Pid>();
                    for (var node = row.Signals.First; node is not null; node = node.Next)
                    {
                        var signal = node.Value;
                        if (signal.Caller is { } caller && blocked.Contains(caller)) continue;
                        if (signal.WaitsForBusy && row.ManualBusy
                            && !row.Closing && !row.StopRequested && !row.PhysicallyStopped)
                        {
                            if (signal.Caller is { } waiting) blocked.Add(waiting);
                            continue;
                        }
                        next = signal;
                        selected = node;
                        break;
                    }
                    if (next is null)
                    {
                        if (row.SignalsChanged.Task.IsCompleted)
                            row.SignalsChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
                        changed = Task.WhenAny(row.SignalsChanged.Task, row.Writable.Task);
                    }
                    else row.Signals.Remove(selected!);
                }
                if (next is null)
                {
                    await changed!.ConfigureAwait(false);
                    continue;
                }
                try
                {
                    next.Completion.TrySetResult(await next.Execute().ConfigureAwait(false));
                }
                catch (ErlangException failure)
                {
                    next.Completion.TrySetException(failure);
                }
                catch (Exception failure)
                {
                    next.Completion.TrySetException(failure);
                    throw;
                }
            }
        }
        catch (Exception failure)
        {
            lock (gate)
            {
                row.SignalFailure = failure;
                foreach (var signal in row.Signals) signal.Completion.TrySetException(failure);
                row.Signals.Clear();
                row.SignalsRunning = false;
            }
            throw;
        }
    }
}
