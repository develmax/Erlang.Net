namespace Erlang;

internal sealed class RuntimeEtsOwnerTables(Pid owner)
{
    internal Pid Owner { get; } = owner;
    internal bool Retired { get; set; }
    internal LinkedList<RuntimeEtsFoundationTable> Tables { get; } = new();
    internal LinkedListNode<RuntimeEtsOwnerTables>? GlobalLink { get; set; }
}
