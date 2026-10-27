-module(owned_probe506).
-export([live/1, held/1, capture/1]).
live(Value) -> Value.
capture(Value) -> Value.
held(Value) ->
    whereis(owned_observer506) ! {held, self(), Value},
    receive release -> Value end.
