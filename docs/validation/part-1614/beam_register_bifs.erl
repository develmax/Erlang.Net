-module(beam_register_bifs).
-export([run/3]).
run(A,B,C) ->
    {hd(A),tl(B),tuple_size(C)}.
