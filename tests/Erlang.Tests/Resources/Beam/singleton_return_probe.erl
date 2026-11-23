-module(singleton_return_probe).
-export([choose/1, ret/0, terminal/0, small/0, unobserved/0]).
-record(pair, {alpha,beta}).
choose(a) -> #pair{alpha=small(), beta=big()};
choose(b) -> #pair{alpha=big(), beta=small()}.
ret() -> {small(), big()}.
terminal() -> small().
small() -> 7.
big() -> 340282366920938463463374607431768211456.
unobserved() -> {signal(), big()}.
signal() -> put(probe,called), 7.
