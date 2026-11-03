namespace Erlang;

// Explicit first-increment host custody. Dispose after every returned native
// term/view is no longer used, and before disposing its VM. This is not CLR GC.
public sealed class NativeTermHostScope524 : IDisposable
{
    private static readonly AsyncLocal<NativeTermHostScope524?> current = new();
    private readonly NativeTermHostScope524? previous;
    private readonly ProcessRuntime runtime;
    private readonly List<IDisposable> roots = new();
    private readonly object gate = new();
    private bool disposed;

    public NativeTermHostScope524(ProcessRuntime runtime)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        previous = current.Value;
        current.Value = this;
    }

    internal static Term AdmitCurrent524(Term value)
    {
        NativeTermHostScope524 scope = current.Value
            ?? throw new NotSupportedException(NativeResource524Names.HostLeaseRequired);

        return scope.Admit524(value);
    }

    internal static Term AdmitCurrent524(ProcessRuntime runtime, Term value)
    {
        NativeTermHostScope524? scope = current.Value;
        if (scope is null || !ReferenceEquals(scope.runtime, runtime))
            throw new NotSupportedException(NativeResource524Names.HostLeaseRequired);

        return scope.Admit524(value);
    }

    private Term Admit524(Term value)
    {
        lock (gate)
            ObjectDisposedException.ThrowIf(disposed, this);
        // Resource/heap/domain traversal must not run under the host-scope gate.
        IDisposable? lease = NativeResourceRoots524.RetainGraph524(value, runtime, hostAtomics586: true);
        if (lease is null) return value;
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                roots.Add(lease);
            }
        }
        catch
        {
            lease.Dispose();
            throw;
        }

        return value;
    }

    public void Dispose()
    {
        List<Exception> failures;
        lock (gate)
        {
            if (disposed) return;
            if (!ReferenceEquals(current.Value, this))
                throw new InvalidOperationException(NativeResource524Names.Unbalanced);
            // Reserve all error slots before closing admission. An allocation
            // failure leaves the scope open and its roots available for retry.
            failures = new(roots.Count);
            disposed = true;
        }
        current.Value = previous;
        // Every publisher rechecks the gate after retaining. The list is now
        // stable; resource release/callback dispatch runs outside our monitor.
        foreach (IDisposable lease in roots)
            try
            {
                lease.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        roots.Clear();
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
