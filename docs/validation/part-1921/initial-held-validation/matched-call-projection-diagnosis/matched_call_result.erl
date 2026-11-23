-module(matched_call_result).
-export([project/2, keep/2, produce/1]).
project(Input, Name) ->
    Children = ?MODULE:produce(Input),
    case lists:keysearch(Name, 1, Children) of
        {value, {_, Selected, _, _}} -> {ok, Selected};
        false -> missing
    end.
keep(Input, Name) ->
    Children = ?MODULE:produce(Input),
    case lists:keysearch(Name, 1, Children) of
        {value, {_, Selected, _, _}} -> {ok, Selected};
        false -> {missing, Name}
    end.
produce(V) -> V.
