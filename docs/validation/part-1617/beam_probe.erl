-module(beam_probe).
-export([side/0, accept/1, pair/2]).
side() -> put(side, yes).
pair(left, ok) -> ok;
pair(X,Y) -> {X,Y}.
accept(yes) -> ok;
accept(Value) -> Value.
