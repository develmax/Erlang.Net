-module(beam_branch_frame_3).
-export([run/2]).
run(Value, Mode) -> X=seed(Value), Y=case Mode of left -> A=step(X), {A, step(A)}; right -> B=step(X), {step(B),B}; _ -> X end, {Y,step(Y)}.
seed(X) -> erlang:garbage_collect(), X.
step(X) -> erlang:garbage_collect(), {step, X}.
