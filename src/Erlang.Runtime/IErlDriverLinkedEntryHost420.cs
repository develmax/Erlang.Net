namespace Erlang;

internal interface IErlDriverLinkedEntryHost420
{
    // Parent prepends an actual linked-in registry descriptor and returns its
    // own identity. This may be called recursively during another entry.init.
    ulong Register(ErlDriverLinkedEntryRequest420 entry);

    void Remove(ulong identity);
}
