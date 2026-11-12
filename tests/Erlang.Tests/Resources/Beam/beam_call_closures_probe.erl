-module(beam_call_closures_probe).
-export([nested/2, captured/2, direct/2, tail/2, reuse/2]).

nested(Key, Pairs) ->
    Values = proplists:get_value(Key, Pairs),
    {ok, lists:reverse(lists:map(fun(Value) -> Value end, lists:sort(Values)))}.

captured(Key, Pairs) ->
    Values = proplists:get_value(Key, Pairs),
    {ok, lists:reverse(lists:map(fun(_) -> Values end, lists:sort(Values)))}.

direct(Key, Pairs) ->
    Values = proplists:get_value(Key, Pairs),
    {ok, lists:map(fun(_) -> Key end, Values)}.

tail(Key, Pairs) ->
    Values = proplists:get_value(Key, Pairs),
    lists:map(fun(Value) -> Value end, lists:sort(Values)).

reuse(Key, Pairs) ->
    Values = proplists:get_value(Key, Pairs),
    {ok, lists:reverse(lists:map(fun(_) -> Key end, lists:flatten([Values])))}.
