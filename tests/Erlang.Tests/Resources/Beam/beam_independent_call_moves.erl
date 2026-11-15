-module(beam_independent_call_moves).
-export([run/1]).
run({Value}) -> {Value ++ ".parse", Value}.
