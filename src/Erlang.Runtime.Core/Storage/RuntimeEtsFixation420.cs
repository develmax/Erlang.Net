namespace Erlang;

internal sealed class RuntimeEtsFixation420(Pid process, RuntimeEtsFoundationTable table)
{
    internal Pid Process { get; } = process;
    internal RuntimeEtsFoundationTable Table { get; } = table;
    internal long Count { get; set; } = 1;
    internal bool Active { get; set; } = true;
    internal LinkedListNode<RuntimeEtsFixation420>? TableLink { get; set; }
    internal LinkedListNode<RuntimeEtsFixation420>? ProcessLink { get; set; }
}
