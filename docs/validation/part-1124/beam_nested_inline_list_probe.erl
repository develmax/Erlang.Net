-module(beam_nested_inline_list_probe).
-export([run/1, forward/2, same/1, dead/3, dead_same/2]).
-compile({inline, checked/1}).
run(Value) ->
    Kept = checked(Value),
    {ok, Kept}.
forward(Value, Other) ->
    Kept = checked(Value),
    finish(hd(Other), Kept).
same(Value) ->
    Kept = checked(Value),
    finish(hd(Kept), Kept).
dead(_Ignored, Value, Other) ->
    Kept = checked(Value),
    finish(hd(Other), Kept).
dead_same(_Ignored, Value) ->
    Kept = checked(Value),
    finish(hd(Kept), Kept).
finish(Head, Value) -> {Head, Value}.
checked(Value) ->
    [Head|Tail] = Value,
    [begin Head end|Tail].

