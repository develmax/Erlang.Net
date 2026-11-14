-module(beam_reused_closure_frame).
-export([run/3]).
run(A,B,C) ->
    F = fun(X) -> join(X,B,C) end,
    call(A,F).
join(X,B,C)->{X,B,C}.
call(A,F)->F(A).
