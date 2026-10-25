namespace Erlang;

internal sealed class RuntimeEtsObjectSlot(long id, RuntimeEtsStoredObject payload)
{
    internal long Id { get; } = id;
    internal RuntimeEtsStoredObject Payload { get; } = payload;
    internal LinkedListNode<RuntimeEtsObjectSlot>? Link { get; set; }
}
