namespace Erlang;

// Owns a real native environment reference. Never exports a native pointer,
// MemoryManager or ReadOnlyMemory that can outlive this admitted lease.
internal sealed class NifBinaryPayloadLease512 : IDisposable
{
    private readonly object gate = new();
    private NifNativeBinaryStorage? storage;

    internal NifBinaryPayloadLease512(NifNativeBinaryStorage storage)
    {
        storage.RetainEnvironment();
        this.storage = storage;
    }

    internal nuint Size512
    {
        get
        {
            lock (gate)
                return (storage ?? throw new ObjectDisposedException(nameof(NifBinaryPayloadLease512))).Size;
        }
    }

    // The explicit copy is an interop boundary; managed refc root tracking never
    // calls it or copies an existing BinaryBacking payload.
    internal byte[] Copy512(nuint offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        lock (gate)
        {
            var current = storage ?? throw new ObjectDisposedException(nameof(NifBinaryPayloadLease512));
            nuint length = current.Size;
            if (offset > length || (nuint)count > length - offset)
                throw new InvalidOperationException(NifNativeBinaryNames.SubBinaryRange);
            byte[] result = new byte[count];
            current.CopyPayload512(offset, result);

            return result;
        }
    }

    public void Dispose()
    {
        NifNativeBinaryStorage? previous;
        lock (gate)
        {
            previous = storage;
            storage = null;
        }
        previous?.ReleaseEnvironment();
    }
}
