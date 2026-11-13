-module(beam_case_projected_tail).
-export([lookup/2]).

lookup(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        false -> Key;
        {value, {_, Next}} -> lookup(Next, Rows)
    end.
