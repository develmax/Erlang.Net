-module(beam_probe).
-export([pass/1]).
pass(X) -> put(trace,X), X.
