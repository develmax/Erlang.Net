-module(general_cons_probe).
-export([run_single/1, run_shared/2, run_tail/2]).

run_single(Tail) -> single([head|Tail]).
run_shared(Tag, Map) -> both([Tag, Map], key).
run_tail(Head, Tail) -> tail([Head|Tail]).

single([First, Second|Tail]) ->
    {First, Second, Tail, length(Tail)}.

both([first|Tail], Key) ->
    {first, maps:get(Key, hd(Tail))};
both([second|Tail], Key) ->
    {second, maps:get(Key, hd(Tail))}.

tail([Head|Tail]) -> finish(Tail, Head).
finish(Tail, Head) -> {Tail, Head}.
