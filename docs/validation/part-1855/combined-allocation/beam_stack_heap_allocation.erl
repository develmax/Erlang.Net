-module(beam_stack_heap_allocation).
-export([run/2]).
run(X, Y) ->
    Value = other:consume([X, Y]),
    {Value, Y}.
