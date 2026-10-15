namespace Erlang;

internal static class NifNativeLifecycleGate
{
    private static readonly SemaphoreSlim gate = new(NifNativeLifecycleNames.SingleOperation, NifNativeLifecycleNames.SingleOperation);
    [ThreadStatic]
    private static bool entered;

    internal static IDisposable Enter(CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (entered)
            throw new InvalidOperationException(NifNativeLifecycleNames.ReentrantOperation);
        // Allocation must precede acquiring ownership, so an allocation failure cannot leak the permit.
        var scope = new Scope();
        gate.Wait(cancellation);
        entered = true;

        return scope;
    }

    private sealed class Scope : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            entered = false;
            gate.Release();
        }
    }
}
