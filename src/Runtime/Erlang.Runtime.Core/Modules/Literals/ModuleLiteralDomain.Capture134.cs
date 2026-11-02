namespace Erlang;

internal sealed partial class ModuleLiteralDomain800
{
    // Exact live registered lease, including after logical code-owner closure.
    // Domain metadata alone and disposed/unregistered leases grant nothing.
    internal bool IsRegisteredCaptureHolder134(ProcessRuntime expectedRuntime, ModuleLiteralLease800 lease)
    {
        lock (gate)
            return sealed800 && !retiring && ReferenceEquals(runtime, expectedRuntime)
                && ReferenceEquals(lease.Domain, this) && !lease.IsDisposed && holders.Contains(lease);
    }
}
