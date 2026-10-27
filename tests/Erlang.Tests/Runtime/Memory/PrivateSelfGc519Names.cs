namespace Erlang.Tests;

internal static class PrivateSelfGc519Names
{
    internal const string BifTest = "runtime/resource519/real-garbage-collect-reclaims-unrooted-owned-cells";
    internal const string LiveTest = "runtime/resource519/real-garbage-collect-keeps-hidden-map-binary-live";
    internal const string OrdinaryTest = "runtime/resource519/ordinary-garbage-collect-does-not-create-owned-heap";
    internal const string ErlangModule = "erlang";
    internal const string GarbageCollect = "garbage_collect";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string SetLocal = "SetLocalGraph507";
    internal const string Read = "Read";
    internal const string Clear = "Clear";
    internal const string HeapField = "termHeap506";
    internal const string Registry = "BinaryRegistry512";
    internal const string BinarySnapshot = "Snapshot512";
    internal const string ReferenceBytes = "ReferenceBackingBytes";
    internal const int BackingBytes = 65;
    internal const int SeedMapSize = 33;
    internal const string ReclaimDiagnostic = "{0} Actual owned payload before={1}, after={2}.";
    internal const string Test = "runtime/resource519/explicit-private-self-gc-clears-dead-slabs-preserves-live";
    internal const string GuestModule = "private_gc519";
    internal const string Probe = "private_gc_probe519";
    internal const string Sample = "sample";
    internal const string Discarded = "discarded";
    internal const string Kept = "kept";
    internal const string Heap = "TermHeap506";
    internal const string Snapshot = "Snapshot";
    internal const string Words = "LivePayloadWords";
    internal const string Failure = "Private self GC519 left dead owned cells or lost live source values.";
    internal const int Count = 256;
    internal const string Source = """
        -module(private_gc519).
        -export([discarded/1, kept/1]).
        discarded(N) ->
            lists:seq(1,N), private_gc_probe519:sample(),
            true=erlang:garbage_collect(), private_gc_probe519:sample().
        kept(N) ->
            L=lists:seq(1,N), true=erlang:garbage_collect(),
            {length(L), hd(L), lists:last(L)}.
        """;
}
