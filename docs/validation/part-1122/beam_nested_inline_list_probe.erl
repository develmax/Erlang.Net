-module(beam_nested_inline_list_probe).
-export([run/1]).
-compile({inline, checked/1}).
run(Value) ->
    Kept = checked(Value),
    {ok, Kept}.
checked(Value) ->
    [Head|Tail] = Value,
    [begin Head end|Tail].

