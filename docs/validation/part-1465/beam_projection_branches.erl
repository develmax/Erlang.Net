-module(beam_projection_branches).
-export([branch/2, retain/2]).
branch({item, Value}, Flag) ->
    case check(Value) of
        yes -> pair(Flag, yes);
        no -> pair(Flag, no)
    end.
retain({item, Value}, Flag) ->
    case check(Value) of
        yes -> pair(Value, Flag);
        no -> pair(no, Flag)
    end.
check(Value) -> Value.
pair(A, B) -> {A, B}.
