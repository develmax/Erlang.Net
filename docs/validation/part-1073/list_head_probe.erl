-module(list_head_probe).
-export([consume/2, literal/1, tail_only/1, no_fields/1]).
consume([Head|Tail], Acc) ->
    Previous = erlang:put(list_head_key, Head),
    consume(Tail, [Previous|Acc]);
consume([], Acc) -> Acc.
literal([Head|_]) -> {value, erlang:put(list_head_key, Head)};
literal([]) -> empty.

tail_only([Unused|Tail]) -> {tail, erlang:put(tail_key, Tail)};
tail_only([]) -> empty.
no_fields([_|_]) -> {both, erlang:self()};
no_fields([]) -> empty.

