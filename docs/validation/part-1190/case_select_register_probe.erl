-module(case_select_register_probe).
-export([read/1, named/1]).
read(Key) ->
    Chosen = case erlang:get(Key) of
        first -> erlang:put(Key, second);
        second -> erlang:put(Key, finished);
        _ -> erlang:put(Key, fallback)
    end,
    {Chosen, erlang:get(Key)}.
named(Key) ->
    Chosen = case erlang:get(Key) of
        first -> erlang:put(Key, second);
        second -> erlang:put(Key, finished);
        Other -> {Other, erlang:put(Key, fallback)}
    end,
    {Chosen, erlang:get(Key)}.
