-module(beam_split_tagged_case).
-export([prepared/2, direct/2, arbitrary/1, fallback/1]).

prepared(Input, Key) ->
    Items = lists:reverse(Input),
    case lists:keysearch(Key, 1, Items) of
        {value, {_, Value, _, _}} -> {ok, Value};
        false -> missing
    end.

direct(Input, Key) ->
    case lists:keysearch(Key, 1, Input) of
        {value, {_, Value, _, _}} -> {ok, Value};
        false -> missing
    end.

arbitrary(Input) ->
    case Input of
        {value, {_, Value, _, _}} -> {ok, Value};
        false -> missing
    end.

fallback(Input) ->
    case Input of
        {value, {_, Value, _, _}} -> {ok, Value};
        false -> missing;
        _ -> bad_value
    end.
