namespace Erlang;

// Storage metadata only: never retains a source BitString wrapper or Term graph.
internal sealed class PersistentBinaryBacking416 : IDisposable
{
    private readonly BinaryBacking backing;
    private IDisposable? nativeRoot524;

    internal PersistentBinaryBacking416(BinaryBacking backing)
    {
        this.backing = backing;
        nativeRoot524 = backing.RetainNative524();
    }

    internal object StorageIdentity => backing;

    internal BitString Materialize(long bitOffset, int bitLength)
        => BitString.FromPersistentBacking416(backing, bitOffset, bitLength);

    public void Dispose() => Interlocked.Exchange(ref nativeRoot524, null)?.Dispose();
}
