namespace Erlang;

internal sealed class RuntimeEtsFoundationTable(
    Pid owner,
    Atom name,
    bool named,
    RuntimeEtsProtection protection,
    RuntimeEtsOwnerTables ownership,
    int creationLane420
)
{
    internal int CreationLane420 { get; } = creationLane420;
    internal LinkedListNode<RuntimeEtsFoundationTable>? CreationLink420 { get; set; }
    internal EtsPrimaryIndex420? Primary420 {get;set;}
    internal System.Numerics.BigInteger FixedAtMonotonic420 {get;set;}
    internal System.Numerics.BigInteger FixedOffset420 {get;set;}
    internal long FixedCount420 {get;set;}
    internal Dictionary<Pid,RuntimeEtsFixation420> Fixations420 {get;}=new();
    internal LinkedList<RuntimeEtsFixation420> FixedOrder420 {get;set;}=new();
    internal Pid Owner { get; set; } = owner;
    internal Atom Name { get; set; } = name;
    internal bool Named { get; } = named;
    internal RuntimeEtsProtection Protection { get; set; } = protection;
    internal RuntimeEtsHeir419? Heir419 { get; set; }
    internal ResourceReferenceTerm Handle { get; set; } = null!;
    internal RuntimeEtsOwnerTables Ownership { get; set; } = ownership;
    internal LinkedListNode<RuntimeEtsFoundationTable>? OwnerLink { get; set; }
    internal Dictionary<long, RuntimeEtsObjectSlot> Objects { get; set; } = new();
    internal LinkedList<RuntimeEtsObjectSlot> ObjectOrder { get; set; } = new();
    internal long LastObjectId { get; set; }
    internal bool Retired { get; set; }
}
