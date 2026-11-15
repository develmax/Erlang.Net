-module(beam_entry_bif_placement).
-export([run/1, keep/1]).

run(Value) ->
    Incremented = Value + 1,
    erlang:garbage_collect(),
    {ok, Incremented}.

keep(Value) ->
    Incremented = Value + 1,
    erlang:garbage_collect(),
    {Value, Incremented}.
