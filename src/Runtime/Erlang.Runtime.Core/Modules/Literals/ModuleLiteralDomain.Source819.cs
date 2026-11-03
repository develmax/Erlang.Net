namespace Erlang;

internal sealed partial class ModuleLiteralDomain800
{
    internal bool HasSourceLease819(ModuleLiteralLease800 lease)
    {
        lock (gate)
            return !retiring && !lease.IsDisposed && ReferenceEquals(lease.Domain, this) && holders.Contains(lease);
    }
}
