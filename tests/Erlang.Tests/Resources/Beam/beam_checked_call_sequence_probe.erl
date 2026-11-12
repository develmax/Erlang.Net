-module(beam_checked_call_sequence_probe).
-export([store/1, tail/2, nested/1, checked/1]).

store(Pairs) ->
    {ok, Value} = proplists:get_value(key, Pairs),
    put(key, Value),
    ok.

tail(Key, Pairs) ->
    {ok, More} = proplists:get_value(Key, Pairs),
    {ok, Value} = proplists:get_value(Key, More),
    lists:reverse(Value).

nested(Pairs) ->
    {ok, Value} = proplists:get_value(key, Pairs),
    Sorted = lists:sort(Value),
    {result, lists:reverse(Sorted)}.

checked(Pairs) ->
    {ok, _} = proplists:get_value(key, Pairs),
    ok = proplists:get_value(check, Pairs),
    done.
