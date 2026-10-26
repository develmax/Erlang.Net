namespace Erlang;

// Storage metadata only: never retains a source BitString wrapper or Term graph.
internal sealed class PersistentBinaryBacking416(BinaryBacking backing)
{
    internal object StorageIdentity => backing;

    internal BitString Materialize(long bitOffset, int bitLength)
        => BitString.FromPersistentBacking416(backing, bitOffset, bitLength);
}
