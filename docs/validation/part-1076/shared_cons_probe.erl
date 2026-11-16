-module(shared_cons_probe).
-export([read/2, head/1, tail/1, shape/1]).
read([{tag, Value}|Tail], Acc) when is_integer(Value) ->
    {integer, erlang:put(shared_cons_key, Value), Tail, Acc};
read([{tag, Value}|Tail], Acc) ->
    {other, erlang:put(shared_cons_key, Value), Tail, Acc};
read([Value|Value], Acc) -> {equal, Value, Acc};
read([], Acc) -> Acc.

head([{tag, V}|_]) -> {tag, erlang:put(head_key, V)};
head([V|_]) -> {other, erlang:put(head_key,V)}.
tail([_|T]) -> {tail, erlang:put(tail_key,T)}.
shape([_|_]) -> {shape, erlang:self()}.

