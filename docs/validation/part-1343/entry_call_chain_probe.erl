-module(entry_call_chain_probe).
-export([walk/2, forced/2, right/2]).
walk([Head|Tail], Context) ->
    [walk(Head, Context)|walk(Tail, Context)];
walk(Tuple, Context) when is_tuple(Tuple) ->
    list_to_tuple(walk(tuple_to_list(Tuple), Context));
walk(Value, Context) when is_integer(Value) ->
    convert(Value, Context);
walk(Value, _) ->
    Value.
convert(Value, Context) -> {Value,Context}.
forced(Value, Context) ->
    list_to_tuple(walk(tuple_to_list(Value), Context)).
right(Context, [Head|Tail]) ->
    [convert(Head, Context)|right(Context, Tail)];
right(_, []) -> [];
right(Context, Value) ->
    {Value,Context}.
