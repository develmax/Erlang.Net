-module(beam_length_body).
-export([run/3]).
run(A, B, C) ->
    {length(A), erlang:length(B), length(id(C))}.
id(X) -> [X].
