namespace Erlang;

public sealed partial class ProcessContext
{
    internal ProcessGarbageCollectionPolicy GarbageCollectionPolicy { get; } = new();

    internal bool CollectManagedGarbage(bool requestedMajor, Action<bool> collector)
        => GarbageCollectionPolicy.Collect(requestedMajor, collector);
}
