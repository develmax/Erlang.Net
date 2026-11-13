-module(beam_case_tail_saved).
-export([lookup/2, require/2]).

lookup(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> Value;
        false -> missing(Key, Rows)
    end.

require(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> Value;
        false -> throw({missing, Key})
    end.

missing(Key, Rows) ->
    {missing, Key, Rows}.
