-module(tuple_head_probe).
-export([read/2, untagged/1]).
read([{tag, Unused, marker, Value}|Tail], Acc) ->
    {erlang:put(tuple_key, Value), Tail, Acc};
read([], Acc) -> Acc.
untagged([{Value, 42}|_]) -> {value, erlang:put(tuple_key, Value)};
untagged([]) -> empty.
