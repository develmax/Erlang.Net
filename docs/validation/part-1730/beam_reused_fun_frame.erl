-module(beam_reused_fun_frame).
-export([run/2]).
run(A,F) ->
    B = F(A),
    next(B,F).
next(B,F) -> F({wrapped,B}).
