using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class PersistentArenaRead416(
    PersistentPairArena416 owner,
    nint pointer,
    int bytes,
    PersistentArenaRoots416 roots
) : IDisposable
{
    private readonly object gate = new();
    private PersistentPairArena416? owned = owner;
    private PersistentArenaRoots416? bundle = roots;

    internal TupleTerm Materialize(CancellationToken cancellation)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(owned is null, this);
            cancellation.ThrowIfCancellationRequested();
            var copy = new byte[bytes];
            Marshal.Copy(
                pointer,
                copy,
                0,
                bytes
            );

            return (TupleTerm)PersistentArenaCodec416.Decode(copy, bundle!, cancellation);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            PersistentPairArena416? old = owned;
            owned = null;
            bundle = null;
            old?.DangerousRelease();
        }
    }
}
