-module(beam_call_tuple_case_projection).
-export([project/1, lookup/2]).

project(Rows) ->
    case lists:last(Rows) of
        {ok, Value} -> Value;
        {error, {already_started, Value}} -> Value
    end.

lookup(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {_Key, Value}} -> Value;
        _ -> undefined
    end.
