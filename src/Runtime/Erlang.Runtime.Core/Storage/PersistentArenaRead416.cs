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
        => MaterializeSelected591(cancellation, true, false);

    internal TupleTerm MaterializeSelected591(CancellationToken cancellation, bool admitResults = true, bool valueOnly = false)
        => MaterializeSelected594(
            cancellation,
            admitResults,
            valueOnly,
            null
        );

    internal TupleTerm MaterializeSelected594(
        CancellationToken cancellation,
        bool admitResults,
        bool valueOnly,
        AtomicsHostCopyTransaction591? transaction
    )
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

            return (TupleTerm)PersistentArenaCodec416.DecodeSelected594(
                copy,
                bundle!,
                cancellation,
                admitResults,
                valueOnly,
                transaction
            );
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
