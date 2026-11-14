-module(beam_reused_constructor_slot).
-export([run/1, finish/2]).
run(Key) ->
    finish(filename:rootname(Key), lists:reverse([{{tag,Key},data}])).
finish(First, Second) ->
    {First, Second}.
