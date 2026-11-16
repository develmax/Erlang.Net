-module(call_operand_lifetime_probe).
-export([f/2, recursive/2]).
identity(V) -> V.
f(Fun, Value) ->
    Called = (identity(Fun))(identity(Value)),
    Sent = identity(self()) ! identity(Called),
    {Sent, identity(9) / identity(2), identity(Value)}.

recursive(N, Seed) ->
    F = fun Loop(0) -> Seed;
            Loop(K) -> {Loop(identity(K - 1)), identity(K)}
        end,
    F(N).