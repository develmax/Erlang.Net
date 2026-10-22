namespace Erlang;

internal sealed partial class RuntimeEtsStoredObject
{
    internal Term MaterializeKey438(long position, CancellationToken cancellation)
    {
        var owned = Volatile.Read(ref payload) ?? throw new ObjectDisposedException(nameof(RuntimeEtsStoredObject));

        return owned.MaterializeKey438(position, cancellation);
    }
}
