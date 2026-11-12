-module(container_operand_lifetime_probe).
-export([f/2]).
identity(V) -> V.
f(Value, Tail) ->
    First = {identity(Value), Bound = identity(Tail)},
    Second = [identity(Value), Next = identity(Bound) | identity(Tail)],
    Third = {identity(Next), identity(Value)},
    {First, Second, Third, Bound, Next}.

