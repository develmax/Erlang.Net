namespace Erlang;

internal sealed partial class RuntimeEtsReadLease
{
    internal Term MaterializeKey438(long position, CancellationToken cancellation)
    {
        lock (gate)
        {
            var current = owner ?? throw new ObjectDisposedException(nameof(RuntimeEtsReadLease));

            return current.MaterializeKey438(position, cancellation);
        }
    }
}
