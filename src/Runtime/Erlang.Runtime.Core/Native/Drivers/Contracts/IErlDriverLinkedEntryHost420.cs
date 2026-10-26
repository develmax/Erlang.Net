namespace Erlang;

internal interface IErlDriverLinkedEntryHost420
{
    // Parent prepends an actual linked-in registry descriptor and returns its
    // own identity. This may be called recursively during another entry.init.
    string Session438 { get; }

    ErlDriverLinkedScope438 BeginInitialization438(ulong ownerImage);

    void EndInitialization438(ErlDriverLinkedScope438 scope, bool cleanupSucceeded);

    ulong Register(ErlDriverLinkedEntryRequest420 entry, ErlDriverLinkedScope438 scope);

    void Remove(ulong identity, ErlDriverLinkedScope438 scope);
}
