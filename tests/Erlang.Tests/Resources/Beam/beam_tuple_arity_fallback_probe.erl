-module(beam_tuple_arity_fallback_probe).
-export([pick/2, forward/2]).

pick(_, {left,Value}) ->
    Value;
pick(_, {right,_,Value}) ->
    Value;
pick(Fallback, _) ->
    Fallback.

forward(_, {left,Value}) ->
    Value;
forward(_, {right,_,Value}) ->
    Value;
forward(Value, Items) ->
    lists:member(Value, Items).
