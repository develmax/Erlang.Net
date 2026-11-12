-module(beam_constant_append).
-export([run/0]).
run() -> {"a" ++ "b", [1,2] ++ tail}.
