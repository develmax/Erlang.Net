-module(bound).
-export([bound/3]).
bound(Key, Map, Default) ->
    Value = maps:get(Key, Map, Default),
    erlang:put(saved_value, Value),
    {ok,Value}.

