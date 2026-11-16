-module(beam_case_tail_raise).
-export([lookup/2, require/2]).

lookup(Key, Rows) ->
    try find(Key, Rows)
    catch throw:missing -> absent
    end.

require(Key, Rows) ->
    try observed(Key, Rows)
    catch throw:missing:Stack -> {absent, Stack}
    end.

find(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> Value;
        false -> throw(missing)
    end.

observed(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> Value;
        false -> throw(missing)
    end.
