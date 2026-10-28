namespace Erlang;

internal sealed class PersistentArenaRoot416(PersistentArenaRootKind416 kind, object value) : IDisposable
{
    private object? owned = value;
    internal PersistentArenaRootKind416 Kind { get; } = kind;

    internal BitString Binary(long offset, int length)
        => (BitString)OwnedTermInvocation506.AdmitNativeStoredResult524(
            Require<PersistentBinaryBacking416>(PersistentArenaRootKind416.Binary).Materialize(offset, length));

    internal Term Identifier(byte[] bytes) => Require<PersistentIdentifierRoot416>(PersistentArenaRootKind416.Identifier).Materialize(bytes);

    internal Term Identity() => OwnedTermInvocation506.AdmitNativeStoredResult524(
        Require<RuntimeExternalTermReservation>(PersistentArenaRootKind416.Identity).Value);

    internal FunctionTerm Function(IReadOnlyList<Term> environment)
        => Require<IRuntimeStoredFunctionRecipe>(PersistentArenaRootKind416.Function).Rebind(environment);

    private T Require<T>(PersistentArenaRootKind416 expected)
    {
        object value = Volatile.Read(ref owned) ?? throw new ObjectDisposedException(PersistentArenaNames416.Closed);
        if (Kind != expected || value is not T result) throw new InvalidDataException(PersistentArenaNames416.RootKind);

        return result;
    }

    public void Dispose()
    {
        object? old = Interlocked.Exchange(ref owned, null);
        if (old is IDisposable reservation) reservation.Dispose();
        // Native backing reservations are real disposable allocation roots.
    }
}
