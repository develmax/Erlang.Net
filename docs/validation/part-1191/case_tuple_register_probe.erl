-module(case_tuple_register_probe).
-export([read/1]).
read(Key) ->
    Chosen = case erlang:get(Key) of
        {first, Value} -> erlang:put(Key, {second, Value});
        {second, Value} -> erlang:put(Key, {finished, Value});
        _ -> erlang:put(Key, fallback)
    end,
    {Chosen, erlang:get(Key)}.
