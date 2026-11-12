-module(general_comparison_operands_probe).
-export([f/3]).
identity(V) -> V.
f(X, Y, Which) ->
    case identity(Which) of
        equal -> identity(X) =:= 4;
        not_equal -> X =/= identity(Y);
        order -> {X < 4, 4 =< Y, X > Y, X >= 4};
        guard when (X =:= 4) xor (Y =< 5) -> matched;
        guard -> unmatched
    end.
