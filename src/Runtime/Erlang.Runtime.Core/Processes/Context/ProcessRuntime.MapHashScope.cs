namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal OtpMapRuntimeIdentityProvider MapHashIdentityProvider(
        Action<MapTerm, OtpMapHashState64, Func<Term, ulong>> appendMap
    )
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);

            return new(
                this,
                LocalIdentity.Snapshot,
                RuntimeMapHashDomain.For(this),
                appendMap
            );
        }
    }

    internal T ReadMapHashIdentity<T>(Func<T> read)
    {
        using var atomScope = new AtomNameScope(AtomNames);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);

            return read();
        }
    }

    internal void RequireMapHashOpen()
    {
        lock (gate)
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);
    }
}
