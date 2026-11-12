-module(cons_nil_calls_probe).
-export([collect/2, prepend/2]).
collect([{_, Value} | Tail], Acc) ->
    collect(Tail, [Value | Acc]);
collect([], Acc) ->
    lists:reverse(Acc).
prepend([Head | Tail], Acc) ->
    prepend(Tail, [{tag, Head} | Acc]);
prepend([], Acc) ->
    finish(Acc).
finish(Acc) -> Acc.
