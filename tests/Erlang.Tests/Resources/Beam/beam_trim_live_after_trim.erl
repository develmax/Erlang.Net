-module(beam_trim_live_after_trim).
-export([run/5, consume/2]).
run(Input, Drop, Second, Keep, Last) ->
    ?MODULE:consume(Input, Last),
    ?MODULE:consume(Input, Keep),
    ?MODULE:consume(Input, Second),
    ?MODULE:consume(Input, Drop),
    ?MODULE:consume(Keep, Input),
    {done, Keep, Input}.
consume(Key, Value) ->
    erlang:put(Key, {value, Value}).
