-module(beam_zip_list_probe).
-export([relaxed/2,mixed/2]).
relaxed(A,B) -> [{X,Y} || {ok,X} <- A && Y <- B, Y > 0].
mixed(A,B) -> [X || {ok,X} <- A && X <:- B].
