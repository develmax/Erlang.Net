-module(beam_call_argument_swap).
-export([run/2, chain/2]).
run(First, Last) ->
    Value = first(First),
    Next = lists:append(Last, []),
    {Value, Next}.
chain(First, Last) ->
    Value = first(First),
    Next = lists:append(Last, []),
    finish(Value, Next).
first(Value) -> {first, Value}.
finish(First, Last) -> {First, Last}.