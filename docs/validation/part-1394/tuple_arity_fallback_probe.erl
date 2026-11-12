-module(tuple_arity_fallback_probe).
-export([pick/2, read/2, forward/2, keep/2, relay/2]).

pick(_, {left,Value}) ->
    Value;
pick(_, {right,_,Value}) ->
    Value;
pick(Fallback, _) ->
    Fallback.

read(Key, {pair,Key}) ->
    exact;
read(Key, {triple,Key,Value}) when is_atom(Value) ->
    {Key,Value};
read(Fallback, Other) ->
    {Fallback,Other}.

forward(_, {left,Value}) ->
    Value;
forward(_, {right,_,Value}) ->
    Value;
forward(Value, Items) ->
    lists:member(Value, Items).

keep(_, {left,_}=Whole) ->
    Whole;
keep(_, {right,_,_}=Whole) ->
    Whole;
keep(Fallback, _) ->
    Fallback.

relay(_, {left,Value}) ->
    relay_target(first, second, Value);
relay(_, {right,_,Value}) ->
    relay_target(first, second, Value);
relay(Fallback, _) ->
    Fallback.

relay_target(A, B, C) ->
    {A,B,C}.
