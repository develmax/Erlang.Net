namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    // Bootstrap serialization only. Workers never acquire this lock.
    private readonly object workerStartupGate = new();
    private readonly TaskCompletionSource normalWorkerStartupSettled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool normalWorkerStartupAttempted;

    internal void StartNormalWorkers()
    {
        if (Monitor.IsEntered(gate))
            throw new InvalidOperationException(RuntimeWorkerStartupNames.DispatcherGate);
        lock (workerStartupGate)
        {
            if (Volatile.Read(ref normalWorkersStarted))
                return;
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(retiring, this);
                normalWorkerStartupAttempted = true;
            }
            try
            {
                foreach (RuntimeNormalWorker worker in normalWorkers)
                    worker.Start();
                // Ready is completed ONLY from the actual worker after thread accounting attach.
                Task.WhenAll(normalWorkers.Select(worker => worker.Ready)).GetAwaiter().GetResult();
                lock (gate)
                {
                    ObjectDisposedException.ThrowIf(retiring, this);
                    Volatile.Write(ref normalWorkersStarted, true);
                    // This marks the bootstrap operation, NOT any simulated worker state.
                    normalWorkerStartupSettled.TrySetResult();
                }
            }
            catch (Exception startup)
            {
                Exception failure = RollbackNormalWorkerStartup(startup);
                if (ReferenceEquals(failure, startup))
                    throw;
                throw failure;
            }
        }
    }

    private Exception RollbackNormalWorkerStartup(Exception startup)
    {
        if (Monitor.IsEntered(gate))
            throw new InvalidOperationException(RuntimeWorkerStartupNames.DispatcherRollbackGate, startup);
        var failures = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        failures.Enqueue(startup);
        lock (gate)
        {
            retiring = true;
            normalWorkersStopping = true;
        }
        RuntimeNormalWorker[] constructed = normalWorkers.Where(worker => worker is not null).ToArray();
        // Initiate EVERY stop before joining ANY worker; one failure does not skip another.
        foreach (RuntimeNormalWorker worker in constructed)
            try
            {
                worker.Stop();
            }
            catch (Exception failure)
            {
                failures.Enqueue(failure);
            }
        Task[] joins = constructed.Select(async worker =>
        {
            try
            {
                await worker.JoinAsync().ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Enqueue(failure);
            }
        }).ToArray();
        Task.WhenAll(joins).GetAwaiter().GetResult();
        var result = RuntimeWorkerStartupFailure.Combine(failures);
        completion.TrySetException(result);
        normalWorkerStartupSettled.TrySetResult();

        return result;
    }
}
