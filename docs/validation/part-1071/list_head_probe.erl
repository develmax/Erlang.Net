-module(list_head_probe).
-export([consume/2, literal/1]).
consume([Head|Tail], Acc) ->
    Previous = erlang:put(list_head_key, Head),
    consume(Tail, [Previous|Acc]);
consume([], Acc) -> Acc.
literal([Head|_]) -> {value, erlang:put(list_head_key, Head)};
literal([]) -> empty.
