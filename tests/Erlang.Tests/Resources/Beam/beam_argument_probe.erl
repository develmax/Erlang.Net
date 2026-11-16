-module(beam_argument_probe).
-export([first/2, second/2, constant/2, id/1]).
first(X, _) -> X.
second(_, X) -> X.
constant(_Unused, _) -> 42.
id(X) -> X.
