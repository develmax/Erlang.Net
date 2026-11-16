-module(beam_binary_tuple_tail_probe).
-export([run/2]).

run(<<Byte, _/binary>>, Rows) when Byte =:= 9; Byte =:= 32 ->
    {_, A, B} = lists:last(Rows),
    finish(B, A);
run(<<"!">>, Rows) ->
    {A, B} = lists:last(Rows),
    finish(A, B);
run(_, _) ->
    skipped.

finish(A, B) ->
    {A, B}.
