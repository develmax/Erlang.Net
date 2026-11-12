-module(list_head_args_probe).
-export([find/3, tuple_value/2]).
find(Key, [Key | Rest], _) ->
    Rest;
find(Key, [_ | Rest], Other) ->
    find(Other, Rest, Key);
find(_, [], _) ->
    missing.
tuple_value(Key, [{tag, Key, Value} | _]) ->
    Value;
tuple_value(Key, [_ | Rest]) ->
    tuple_value(Key, Rest);
tuple_value(_, []) ->
    missing.
