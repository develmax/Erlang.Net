namespace Erlang;

// Seven real bodies; info reads actual authoritative own-VM arena extents.
internal static class PersistentTermNative
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (owner is null || module != PersistentTermNativeNames.Module)
        {
            body = null!;

            return false;
        }
        ErlangFunction? operation = (function, arity) switch
        {
            (PersistentTermNativeNames.Info, 0) => (context, _) => ValueTask.FromResult(owner.PersistentTerms.Info(context)),
            (PersistentTermNativeNames.Get, 0) => (context, _) => ValueTask.FromResult(owner.PersistentTerms.GetAll(context)),
            (PersistentTermNativeNames.Get, 1) => (context, args) => ValueTask.FromResult(owner.PersistentTerms.Get(context, args[0])),
            (PersistentTermNativeNames.Get, 2) => (context, args) => ValueTask.FromResult(owner.PersistentTerms.Get(context, args[0], args[1])),
            (PersistentTermNativeNames.Put, 2) => (context, args) => owner.PersistentTerms.Put(
                context,
                args[0],
                args[1],
                false
            ),
            (PersistentTermNativeNames.PutNew, 2) => (context, args) => owner.PersistentTerms.Put(
                context,
                args[0],
                args[1],
                true
            ),
            (PersistentTermNativeNames.Erase, 1) => (context, args) => owner.PersistentTerms.Erase(context, args[0]),
            _ => null
        };
        if (operation is null)
        {
            body = null!;

            return false;
        }
        body = (context, args) =>
        {
            if (!ReferenceEquals(context.Runtime, owner))
                throw new InvalidOperationException(PersistentTermNativeNames.ForeignOwner);
            owner.RequirePersistentActor(context);

            return operation(context, args);
        };

        return true;
    }
}
