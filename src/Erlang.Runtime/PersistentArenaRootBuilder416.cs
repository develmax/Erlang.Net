namespace Erlang;

internal sealed class PersistentArenaRootBuilder416 : IDisposable
{
    private readonly List<PersistentArenaRoot416> roots = new();
    private readonly Dictionary<object, int> binaries = new(ReferenceEqualityComparer.Instance);
    private bool transferred;

    internal int Binary(PersistentBinaryBacking416 value)
    {
        if (binaries.TryGetValue(value.StorageIdentity, out int existing)) return existing;
        int index = Add(new(PersistentArenaRootKind416.Binary, value));
        binaries.Add(value.StorageIdentity, index);

        return index;
    }

    internal int Identifier(ProcessRuntime owner, Term value)
    {
        var captured = PersistentIdentifierRoot416.Capture(owner, value);
        try
        {
            return Add(new(PersistentArenaRootKind416.Identifier, captured));
        }
        catch
        {
            captured.Dispose();
            throw;
        }
    }

    internal int Identity(ProcessRuntime owner, Term value)
    {
        RuntimeExternalTermReservation reservation = owner.ReserveExternalTerm(value);
        try
        {
            var root = new PersistentArenaRoot416(PersistentArenaRootKind416.Identity, reservation);

            return Add(root);
        }
        catch
        {
            reservation.Dispose();
            throw;
        }
    }

    internal int Function(ProcessRuntime owner, FunctionTerm value)
        => Add(new(PersistentArenaRootKind416.Function, RuntimeStoredFunctionRecipes.Capture(owner, value)));

    private int Add(PersistentArenaRoot416 root)
    {
        try
        {
            int index = roots.Count;
            roots.Add(root);

            return index;
        }
        catch
        {
            root.Dispose();
            throw;
        }
    }

    internal PersistentArenaRoots416 Seal()
    {
        var result = new PersistentArenaRoots416(roots.ToArray());
        transferred = true;

        return result;
    }

    public void Dispose()
    {
        Exception? failure = null;
        if (!transferred)
            foreach (var root in roots)
                try
                {
                    root.Dispose();
                }
                catch (Exception error)
                {
                    failure ??= error;
                }
        roots.Clear();
        binaries.Clear();
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
