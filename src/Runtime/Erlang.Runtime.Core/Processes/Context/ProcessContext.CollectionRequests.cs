namespace Erlang;

public sealed partial class ProcessContext
{
    // Membership, state and drain flag are protected by Runtime's existing gate.
    internal LinkedList<ProcessGarbageCollectionRequest> ReadyGarbageCollections { get; } = new();
    internal bool DrainingGarbageCollections { get; set; }
}
