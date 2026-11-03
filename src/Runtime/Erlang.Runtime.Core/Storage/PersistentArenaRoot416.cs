namespace Erlang;

internal sealed class PersistentArenaRoot416(PersistentArenaRootKind416 kind, object value) : IDisposable
{
    private object? owned = value;
    internal PersistentArenaRootKind416 Kind { get; } = kind;

    internal BitString Binary(long offset, int length) => BinarySelected591(offset, length, true);

    internal BitString BinarySelected591(long offset, int length, bool admitResult)
    {
        BitString value = Require<PersistentBinaryBacking416>(PersistentArenaRootKind416.Binary).Materialize(offset, length);

        return admitResult ? (BitString)OwnedTermInvocation506.AdmitNativeStoredResult524(value) : value;
    }

    internal Term Identifier(byte[] bytes) => Require<PersistentIdentifierRoot416>(PersistentArenaRootKind416.Identifier).Materialize(bytes);

    internal Term Identity() => IdentitySelected591(true);

    internal Term IdentitySelected591(bool admitResult) => IdentitySelected594(admitResult, null);

    internal Term IdentitySelected594(bool admitResult, AtomicsHostCopyTransaction591? transaction)
    {
        Term value = Require<RuntimeExternalTermReservation>(PersistentArenaRootKind416.Identity).Value;

        return admitResult ? OwnedTermInvocation506.AdmitNativeStoredResultSelected594(value, transaction) : value;
    }

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
