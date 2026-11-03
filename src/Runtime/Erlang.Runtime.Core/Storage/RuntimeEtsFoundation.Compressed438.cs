namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    private static Term CoreReadKey438(
        RuntimeEtsStoredObject value,
        long position,
        CancellationToken cancellation,
        AtomicsHostCopyTransaction591 transaction
    )
    {
        using var lease = value.Acquire();

        return lease.MaterializeKeySelected594(position, cancellation, transaction);
    }
}
