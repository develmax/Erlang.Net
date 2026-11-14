-module(beam_append_lines).
-export([run/3]).
run(A, B, C) ->
    {A ++ " " ++ B ++ C, ok}.
