namespace Erlang;

public sealed partial class FunctionTerm
{
    private IOwnedFunctionStorage? ownedFunctionStorage;
    internal IOwnedFunctionStorage? OwnedFunctionStorage => ownedFunctionStorage;

    internal bool BindHeapFunctionStorage(IOwnedFunctionStorage storage)
        => Interlocked.CompareExchange(ref ownedFunctionStorage, storage, null) is null;

    internal void RollbackHeapFunctionStorage(IOwnedFunctionStorage storage)
        => Interlocked.CompareExchange(ref ownedFunctionStorage, null, storage);
}
