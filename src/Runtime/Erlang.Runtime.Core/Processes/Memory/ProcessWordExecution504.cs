namespace Erlang;

internal sealed class ProcessWordExecution504(ProcessOwnedWordArena504 arena) : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly HashSet<ProcessOwnedWordLease504> leases = new();
    private readonly TaskCompletionSource drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int operations;
    private bool closing;
    private bool closed;

    internal ProcessOwnedWordLease504 Allocate(int words)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            ProcessOwnedWordLease504 lease = arena.Allocate(words);
            try
            {
                leases.Add(lease);

                return lease;
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }
    }

    internal IDisposable Enter()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            var operation = new ProcessWordOperation504(this);
            operations = checked(operations + 1);

            return operation;
        }
    }

    internal void Release(ProcessOwnedWordLease504 lease)
    {
        lock (gate)
        {
            if (leases.Remove(lease))
                lease.Dispose();
        }
    }

    internal void Leave()
    {
        lock (gate)
        {
            operations--;
            FinishIfDrained();
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (gate)
        {
            closing = true;
            FinishIfDrained();

            return new(drained.Task);
        }
    }

    private void FinishIfDrained()
    {
        if (!closing || operations != 0 || closed)
            return;
        foreach (ProcessOwnedWordLease504 lease in leases)
            lease.Dispose();
        leases.Clear();
        closed = true;
        drained.TrySetResult();
    }
}
