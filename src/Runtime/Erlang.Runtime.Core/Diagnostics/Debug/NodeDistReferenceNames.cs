namespace Erlang;

internal static class NodeDistReferenceNames
{
    internal const string Query = "node_and_dist_references";
    internal const string Nodes = "node_references";
    internal const string Distributions = "dist_references";
    internal const string Process = "process";
    internal const string Port = "port";
    internal const string Ets = "ets";
    internal const string MatchSpec = "match_spec";
    internal const string Heap = "heap";
    internal const string Link = "link";
    internal const string Monitor = "monitor";
    internal const string Binary = "binary";
    internal const string Timer = "timer";
    internal const string System = "system";
    internal const string Signal = "signal";
    internal const string Node = "node";
    internal const string Control = "control";
    internal const string Sequence = "sequence";
    internal const string Persistent = "persistent_term";
    internal static readonly string[] NodeKinds = [Signal, System, Timer, Binary, Ets, Monitor, Link, Heap];
    internal static readonly string[] DistKinds = [Signal, Sequence, System, Ets, Heap, Control, Node];
}
