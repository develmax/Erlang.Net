-module(case_register_selector_probe).
-export([read/1]).
read(Key) ->
    Chosen = case erlang:get(Key) of
        selected -> erlang:put(Key, matched);
        _ -> erlang:put(Key, fallback)
    end,
    {Chosen, erlang:get(Key)}.
