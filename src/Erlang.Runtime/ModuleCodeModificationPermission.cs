namespace Erlang;

internal sealed class ModuleCodeModificationPermission
{
    private readonly SemaphoreSlim admission = new(1, 1);
    private readonly object registryGate;
    private int pendingSynchronousWriters;
    private int pendingAsynchronousWriters;
    [ThreadStatic]
    private static ModuleCodeModificationScope? current;

    internal ModuleCodeModificationPermission(object registryGate) => this.registryGate = registryGate;

    internal int PendingSynchronousWriters => Volatile.Read(ref pendingSynchronousWriters);
    internal bool IsAvailable => admission.CurrentCount != 0;
    internal int PendingAsynchronousWriters => Volatile.Read(ref pendingAsynchronousWriters);

    private bool IsOwned
    {
        get
        {
            for (var scope = current; scope is not null; scope = scope.Previous)
                if (ReferenceEquals(scope.Owner, this))
                    return true;

            return false;
        }
    }

    internal ModuleCodeModificationScope Enter()
    {
        bool nested = IsOwned;
        if (!nested)
        {
            if (NifImageInitializationFlow.HoldsPermission(this))
                throw new NifImageAdmissionException(NifImageAdmissionFailure.ReentrantInitializationOperation);
            if (Monitor.IsEntered(registryGate))
                throw new InvalidOperationException(ModuleCodeModificationNames.GateOrder);
            Interlocked.Increment(ref pendingSynchronousWriters);
            try
            {
                admission.Wait();
            }
            finally
            {
                Interlocked.Decrement(ref pendingSynchronousWriters);
            }
        }
        try
        {
            var scope = new ModuleCodeModificationScope(this, current, !nested);
            current = scope;

            return scope;
        }
        catch
        {
            if (!nested)
                admission.Release();
            throw;
        }
    }

    internal async ValueTask<ModuleCodeModificationTicket> AcquireAsync(CancellationToken cancellation)
    {
        if (IsOwned)
            throw new InvalidOperationException(ModuleCodeModificationNames.NestedAsync);
        if (NifImageInitializationFlow.HoldsPermission(this))
            throw new NifImageAdmissionException(NifImageAdmissionFailure.ReentrantInitializationOperation);
        if (Monitor.IsEntered(registryGate))
            throw new InvalidOperationException(ModuleCodeModificationNames.GateOrder);
        Interlocked.Increment(ref pendingAsynchronousWriters);
        try
        {
            await admission.WaitAsync(cancellation).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref pendingAsynchronousWriters);
        }
        try
        {
            return new(this);
        }
        catch
        {
            admission.Release();
            throw;
        }
    }

    internal ModuleCodeModificationScope EnterOwned()
    {
        var scope = new ModuleCodeModificationScope(this, current, false);
        current = scope;

        return scope;
    }

    internal void Exit(ModuleCodeModificationScope scope)
    {
        if (scope.OwningThread != Environment.CurrentManagedThreadId || !ReferenceEquals(current, scope))
            throw new InvalidOperationException(ModuleCodeModificationNames.ScopeOrder);
        current = scope.Previous;
        if (scope.ReleasesPermission)
            admission.Release();
    }

    internal void Release() => admission.Release();
}
