-module(beam_element_body).
-export([run/3]).
run(A, B, C) ->
    {element(A, B), erlang:element(2, C), element(1, C)}.
