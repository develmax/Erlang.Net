namespace Erlang;

public sealed class RuntimeDebugController432
{
    private readonly object gate = new();
    private readonly LinkedList<RuntimeDebugStop432> queued = new();
    private readonly HashSet<RuntimeDebugStop432> pending = new(ReferenceEqualityComparer.Instance);
    private TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool closed;

    internal RuntimeDebugStop432 Admit(
        ProcessContext context,
        ModuleCodeImage image,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        var stop = new RuntimeDebugStop432(
            this,
            context.Self,
            image,
            function,
            arguments
        );
        var node = new LinkedListNode<RuntimeDebugStop432>(stop);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            context.Cancellation.ThrowIfCancellationRequested();
            TaskCompletionSource next = new(TaskCreationOptions.RunContinuationsAsynchronously);
            pending.Add(stop);
            queued.AddLast(node);
            stop.QueueNode = node;
            TaskCompletionSource signal = changed;
            changed = next;
            signal.TrySetResult();
        }

        return stop;
    }

    public async ValueTask<RuntimeDebugStop432> WaitForStopAsync(CancellationToken cancellation = default)
    {
        while (true)
        {
            Task signal;
            lock (gate)
            {
                if (queued.First is { } node)
                {
                    queued.RemoveFirst();
                    node.Value.QueueNode = null;

                    return node.Value;
                }
                ObjectDisposedException.ThrowIf(closed, this);
                signal = changed.Task;
            }
            await signal.WaitAsync(cancellation).ConfigureAwait(false);
        }
    }

    internal void Resume(RuntimeDebugStop432 stop)
    {
        lock (gate) if (pending.Contains(stop)) stop.RequestResume();
    }

    internal void ReleaseWait(RuntimeDebugStop432 stop)
    {
        lock (gate)
        {
            if (stop.QueueNode is { } node)
            {
                queued.Remove(node);
                stop.QueueNode = null;
            }
            stop.ClearArguments();
        }
    }

    internal void CompleteStop(RuntimeDebugStop432 stop)
    {
        lock (gate)
        {
            stop.Complete();
            pending.Remove(stop);
        }
    }

    internal Task Close()
    {
        lock (gate)
        {
            // Stage the join before irreversible closure; allocation failure leaves the owner open.
            Task result = Task.WhenAll(pending.Select(stop => stop.Settled));
            closed = true;
            changed.TrySetResult();
            // Real coroutine finally/lease retirement must settle, not just Resume.
            foreach (var stop in pending) stop.RequestClose();

            return result;
        }
    }

    internal Task DrainProcess(Pid process)
    {
        lock (gate) return Task.WhenAll(pending.Where(stop => stop.Process.Equals(process)).Select(stop => stop.Settled));
    }

    internal bool IsDrained
    {
        get
        {
            lock (gate) return pending.Count == 0;
        }
    }
}
