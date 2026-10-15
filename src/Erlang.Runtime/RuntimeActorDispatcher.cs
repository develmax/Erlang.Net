namespace Erlang;

internal sealed class RuntimeActorDispatcher
{
    private readonly object gate = new();
    private readonly ConcurrentExclusiveSchedulerPair pair;
    private readonly TaskFactory factory;
    private int active;
    private bool retiring;

    public int Capacity => pair.ConcurrentScheduler.MaximumConcurrencyLevel;
    public Task Completion => pair.Completion;

    public RuntimeActorDispatcher(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        pair = new(TaskScheduler.Default, capacity, RuntimeActorDispatchParameters.TasksPerTurn);
        factory = new(
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            TaskContinuationOptions.None,
            pair.ConcurrentScheduler
        );
    }

    public Task Dispatch(Func<Task> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);
            active++;
            Task execution;
            try
            {
                execution = factory.StartNew(NifImageInitializationFlow.IsolateActor(body)).Unwrap();
            }
            catch
            {
                active--;
                throw;
            }
            _ = execution.ContinueWith(
                completed => Released(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );

            return execution;
        }
    }

    public Task CompleteWhenIdle()
    {
        lock (gate)
        {
            retiring = true;
            if (active == 0)
                pair.Complete();

            return pair.Completion;
        }
    }

    private void Released()
    {
        lock (gate)
        {
            active--;
            if (retiring && active == 0)
                pair.Complete();
        }
    }
}
