-module(beam_trim_saved_call).
-export([run/2]).

run(Left, Right) ->
    A = lists:reverse(Left),
    B = lists:reverse(Right),
    lists:member(A, B),
    {A, lists:member(A, B)}.
