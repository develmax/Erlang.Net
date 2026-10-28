namespace Erlang.Tests;

internal static class SourceLiveness511Names
{
    internal const string Test = "runtime/owned-source511/statement-roots-closures-and-transactional-builders";
    internal const string GuestModule = "source_liveness511";
    internal const string Probe = "source_probe511";
    internal const string Collect = "collect";
    internal const string Yield = "yield";
    internal const string Ok = "ok";
    internal const string Heap = "TermHeap506";
    internal const string Snapshot = "Snapshot";
    internal const string Words = "LivePayloadWords";
    internal const string HeapCollect = "Collect";
    internal const string Failure = "Source511 physical liveness or whole-value control failed.";
    internal const string Discarded = "discarded";
    internal const string Tail = "tail";
    internal static readonly string[] Entries = [Discarded, "closure", "conditional", "builders", Tail, "transfer"];
    internal const string Source = """
        -module(source_liveness511).
        -export([discarded/0, closure/0, conditional/0, builders/0, tail/0, transfer/0]).
        discarded() ->
            lists:seq(1,512), source_probe511:collect(),
            lists:seq(1,512), source_probe511:collect(), ok.
        closure() ->
            X = {[1,2],[3,4]}, F = fun() -> X end,
            source_probe511:collect(),
            receive after 0 -> ok end,
            try F() after source_probe511:collect() end.
        conditional() ->
            X = {retained,[1,2]}, F = fun() -> X end,
            maybe {ok,V} ?= {ok,F()}, source_probe511:collect(),
                [V || _ <- [1,2]] else _ -> [] end.
        builders() ->
            X = {a,[1,2]},
            T = {source_probe511:yield(X), source_probe511:yield({b,[3,4]})},
            M = #{first => source_probe511:yield(T)}, source_probe511:collect(),
            {T,M,[source_probe511:yield(X) || X <- [a,b]]}.
        tail() -> loop(32,{retained,[1,2]}).
        loop(0,V) -> source_probe511:collect(), V;
        loop(N,V) -> lists:seq(1,64), source_probe511:collect(), loop(N-1,V).
        transfer() ->
            X = {retained,[1,2]},
            try erlang:error({boom,X}) catch error:{boom,V} ->
                source_probe511:collect(), V after source_probe511:collect() end.
        """;
}
