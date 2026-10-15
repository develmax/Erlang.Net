namespace Erlang;

internal sealed class ModuleSourceMutationPermission
{
    private readonly object gate = new();
    private readonly SemaphoreSlim admission = new(1, 1);
    private readonly CancellationTokenSource closing = new();
    private long generation;
    private bool closed;

    internal ModuleRegistry Registry
    {
        get;
    }

    internal ModuleSourceMutationPermission(ModuleRegistry registry, bool initiallyClosed)
    {
        Registry = registry;
        closed = initiallyClosed;
        if (initiallyClosed)
            closing.Cancel();
    }

    internal ModuleSourceMutationTicket TryAcquire()
    {
        lock (gate)
            if (closed)
                throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.RuntimeClosing, Registry);
        if (!admission.Wait(0))
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.StageBusy);

        return CreateTicket(false);
    }

    internal async ValueTask<ModuleSourceMutationTicket> AcquireAsync(CancellationToken cancellation)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation, closing.Token);
        try
        {
            await admission.WaitAsync(stop.Token).ConfigureAwait(false);

            return CreateTicket(true);
        }
        catch (OperationCanceledException) when (closing.IsCancellationRequested)
        {
            throw new ModuleSourceMutationClosedException(Registry, closing.Token);
        }
    }

    private ModuleSourceMutationTicket CreateTicket(bool asynchronous)
    {
        try
        {
            lock (gate)
            {
                if (closed)
                {
                    if (asynchronous)
                        throw new ModuleSourceMutationClosedException(Registry, closing.Token);
                    throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.RuntimeClosing, Registry);
                }
                long next = checked(generation + 1);
                var ticket = new ModuleSourceMutationTicket(this, next);
                generation = next;

                return ticket;
            }
        }
        catch
        {
            admission.Release();
            throw;
        }
    }

    internal bool IsCurrent(long expected)
    {
        lock (gate)
            return generation == expected;
    }

    internal void Close()
    {
        lock (gate)
        {
            if (closed)
                return;
            closed = true;
        }
        closing.Cancel();
    }

    internal void Release() => admission.Release();
}
