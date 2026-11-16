-module(list_clause_probe).
-export([sum/2, private/1, classify/2, repeated/3, follow/2]).

sum([], Acc) ->
    Acc;
sum([Head | Tail], Acc) ->
    Value = step(Head),
    sum(Tail, Value + Acc).

private(Value) ->
    proper([Value], 0).

proper([], Acc) ->
    Acc;
proper([Head | Tail], Acc) ->
    Value = step(Head),
    proper(Tail, Value + Acc).

classify([Head | Tail], Flag) ->
    Value = step(Head),
    case Flag of
        take -> {Value, Tail};
        drop -> ignored
    end;
classify(_, _) ->
    other.

repeated([], _, Acc) ->
    Acc;
repeated([Head | Tail], Head, Acc) ->
    Value = step(Head),
    repeated(Tail, Head, Value + Acc).

follow([], Last) ->
    Last;
follow([Head | Tail], _) ->
    Next = step(Head),
    follow(Tail, Next).

step(Value) ->
    put(trace, [Value | get(trace)]),
    Value.
