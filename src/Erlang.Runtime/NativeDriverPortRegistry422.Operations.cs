namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    private readonly HashSet<Task> operations = new();
    private readonly List<Exception> operationFailures = new();
    private Action<Exception>? reportFailure;

    internal void BindFailure(Action<Exception> value)
    {
        if (Interlocked.CompareExchange(ref reportFailure, value, null) is not null)
            throw new InvalidOperationException(DriverParentWireNames422.Invalid);
    }

    private void Track(Task operation)
    {
        // Caller has not yet released the admission TCS. No callback can run
        // before this tracking transaction has either committed or canceled it.
        _ = operation.ContinueWith(
            static (completed, state) =>
        {
            var owner = (NativeDriverPortRegistry422)state!;
            lock (owner.gate)
            {
                owner.operations.Remove(completed);
                if (completed.Exception is { } failure
                    && failure.Flatten().InnerExceptions.Any(error => error is not ErlangException))
                {
                    owner.operationFailures.Add(failure);
                    owner.reportFailure?.Invoke(failure);
                }
            }
        },
            this,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
        operations.Add(operation);
    }

    internal async Task DrainOperations()
    {
        while (true)
        {
            Task[] pending;
            lock (gate)
            {
                pending = operations.ToArray();
                if (pending.Length == 0) break;
            }
            foreach (Task task in pending)
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (ErlangException) { /* Delivered source refusal. */ }
                catch (Exception) { /* Completion observer retains actual host failures. */ }
            lock (gate)
                foreach (Task task in pending) if (task.IsCompleted) operations.Remove(task);
        }
        lock (gate)
        {
            if (operationFailures.Count == 0) return;
            var failures = operationFailures.ToArray();
            operationFailures.Clear();
            throw new AggregateException(failures);
        }
    }
}
