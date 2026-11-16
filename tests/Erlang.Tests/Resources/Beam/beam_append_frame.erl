-module(beam_append_frame).
-export([value/3, wrapped/3, called/3, reused/2]).
value(A,B,C) ->
    A ++ " " ++ B ++ C.
wrapped(A,B,C) ->
    {A ++ " " ++ B ++ C, ok}.
called(A,B,C) ->
    lists:flatten(A ++ " " ++ B ++ C).
reused(A,B) ->
    A ++ " " ++ A ++ B.
