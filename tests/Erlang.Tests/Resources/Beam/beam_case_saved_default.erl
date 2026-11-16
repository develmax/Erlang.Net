-module(beam_case_saved_default).
-export([lookup/3, lookup_exact/3]).

lookup(Key, Default, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {_, Value}} -> Value;
        false -> Default
    end.

lookup_exact(Key, Default, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> Value;
        false -> Default
    end.
