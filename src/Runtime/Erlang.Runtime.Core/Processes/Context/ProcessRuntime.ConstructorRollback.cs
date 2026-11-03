namespace Erlang;

public sealed partial class ProcessRuntime
{
    private Exception RollbackInitializationWorkers(Exception startup)
    {
        if (System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(RuntimeWorkerStartupNames.RuntimeRollbackGate, startup);
        if (initializer is null)
            return RollbackBeforeInitializationOwner(startup);
        var failures = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        failures.Enqueue(startup);
        CloseConstructorOsEnvironment(failures);
        var drains = new List<Task>();
        // Do not depend on a generic disposal prefix completing successfully before
        // requesting BOTH real worker owners to stop. Each operation is independent.
        Capture(() => DisposeAsync().AsTask());
        Capture(() => actorDispatcher.CompleteWhenIdle());
        DirtyNativeOwner419? dirty;
        lock (gate)
            dirty = dirtyNative419;
        Capture(() => dirty?.BeginClose() ?? Task.CompletedTask);
        Capture(() => DrainOwnedTermContexts506());
        Capture(() => Modules.CloseSourceMutationsAsync());
        try
        {
            Timers?.Dispose();
        }
        catch (Exception failure)
        {
            failures.Enqueue(failure);
        }
        try
        {
            if (ownsStandardStreams)
                StandardStreams?.Dispose();
        }
        catch (Exception failure)
        {
            failures.Enqueue(failure);
        }
        Task[] settled = drains.Select(async task =>
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Enqueue(failure);
            }
        }).ToArray();
        Task.WhenAll(settled).GetAwaiter().GetResult();
        try
        {
            CloseEmptyTuple803().GetAwaiter().GetResult();
        }
        catch (Exception failure)
        {
            failures.Enqueue(failure);
        }

        return RuntimeWorkerStartupFailure.Combine(failures);

        void Capture(Func<Task> operation)
        {
            try
            {
                drains.Add(operation());
            }
            catch (Exception failure)
            {
                failures.Enqueue(failure);
            }
        }
    }

    private Exception RollbackBeforeInitializationOwner(Exception startup)
    {
        var failures = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        failures.Enqueue(startup);
        CloseConstructorOsEnvironment(failures);
        var drains = new List<Task>();
        if (actorDispatcher is { } dispatcher)
            Capture(() => dispatcher.CompleteWhenIdle());
        Capture(() => Modules.CloseSourceMutationsAsync());
        Capture(() => DrainOwnedTermContexts506());
        try
        {
            Timers?.Dispose();
        }
        catch (Exception failure)
        {
            failures.Enqueue(failure);
        }
        try
        {
            if (ownsStandardStreams)
                StandardStreams?.Dispose();
        }
        catch (Exception failure)
        {
            failures.Enqueue(failure);
        }
        Task[] settled = drains.Select(async task =>
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Enqueue(failure);
            }
        }).ToArray();
        Task.WhenAll(settled).GetAwaiter().GetResult();
        try
        {
            CloseEmptyTuple803().GetAwaiter().GetResult();
        }
        catch (Exception failure)
        {
            failures.Enqueue(failure);
        }

        return RuntimeWorkerStartupFailure.Combine(failures);

        void Capture(Func<Task> operation)
        {
            try
            {
                drains.Add(operation());
            }
            catch (Exception failure)
            {
                failures.Enqueue(failure);
            }
        }
    }
}
