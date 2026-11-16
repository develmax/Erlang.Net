-module(shared_cons_probe).
-export([read/2]).
read([{tag, Value}|Tail], Acc) when is_integer(Value) ->
    {integer, erlang:put(shared_cons_key, Value), Tail, Acc};
read([{tag, Value}|Tail], Acc) ->
    {other, erlang:put(shared_cons_key, Value), Tail, Acc};
read([Value|Value], Acc) -> {equal, Value, Acc};
read([], Acc) -> Acc.
