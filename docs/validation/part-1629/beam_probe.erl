-module(beam_probe).
-export([pass/1]).
pass(fail) -> error(failed);
pass(X) -> put(trace, [X | case get(trace) of undefined -> []; T -> T end]), X.
