-module(beam_relocate_before_call).
-export([run/4, consume/2]).
run(Input, Drop, Delayed, Keep) ->
    ?MODULE:consume(Input, Delayed),
    ?MODULE:consume(Input, Drop),
    ?MODULE:consume(Keep, Input),
    {done, Keep, Input}.
consume(Key, Value) ->
    erlang:put(Key, {value, Value}).
