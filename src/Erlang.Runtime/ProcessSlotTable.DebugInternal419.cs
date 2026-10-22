namespace Erlang;

internal sealed partial class ProcessSlotTable
{
    // Caller holds the real runtime table gate. Ascending physical occupied slots,
    // not Dictionary insertion order, matches the source table traversal protocol.
    internal Term DebugList419() => Cons.From(occupied.Select(index => (Term)slots[index]!));
}
