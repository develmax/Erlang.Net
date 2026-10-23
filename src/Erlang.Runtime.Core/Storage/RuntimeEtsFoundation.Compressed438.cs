namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    private static Term CoreReadKey438(RuntimeEtsStoredObject value, long position, CancellationToken cancellation)
    {
        using var lease = value.Acquire();

        return lease.MaterializeKey438(position, cancellation);
    }
}
