namespace Erlang;

internal sealed class DistributionSignalDrain
{
    private static readonly AsyncLocal<DistributionSignalDrainFlow?> current = new();
    private readonly object gate = new();
    private TaskCompletionSource? completed;

    internal bool IsCurrent => current.Value is { Active: true } flow && ReferenceEquals(flow.Owner, this);

    internal void RequireSafeNestedWait()
    {
        if (current.Value is not { Active: true } || IsCurrent)
            return;
        lock (gate)
        {
            if (completed is not null)
                throw new DistributionSignalNestedWaitException();
        }
    }

    internal void Run(Action drain)
    {
        if (IsCurrent)
            return;
        while (true)
        {
            Task? preceding;
            lock (gate)
            {
                preceding = completed?.Task;
                if (preceding is null)
                    completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            if (preceding is null)
                break;
            if (current.Value is { Active: true })
                throw new DistributionSignalNestedWaitException();
            preceding.GetAwaiter().GetResult();
        }
        DistributionSignalDrainFlow? parent = current.Value;
        var flow = new DistributionSignalDrainFlow(this);
        current.Value = flow;
        try
        {
            drain();
        }
        finally
        {
            flow.Active = false;
            current.Value = parent;
            TaskCompletionSource finished;
            lock (gate)
            {
                finished = completed!;
                completed = null;
            }
            finished.TrySetResult();
        }
    }
}
