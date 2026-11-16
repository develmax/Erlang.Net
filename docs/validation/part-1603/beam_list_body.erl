-module(beam_list_body).
-export([run/3]).
run(A, B, C) ->
    {hd(A), erlang:tl(B), hd(C)}.
