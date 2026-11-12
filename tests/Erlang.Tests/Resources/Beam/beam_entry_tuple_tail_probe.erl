-module(beam_entry_tuple_tail_probe).
-export([left/1, right/1, plain/1]).

left(Rows) -> run(left, Rows).
right(Rows) -> run(right, Rows).
plain(Rows) -> run(plain, Rows).

run(left, Rows) ->
    {_, A, B} = lists:last(Rows),
    result(left, B, A);
run(right, Rows) ->
    {_, A, B} = lists:last(Rows),
    result(right, A, B);
run(plain, Rows) ->
    lists:last(Rows).

result(Tag, A, B) ->
    {Tag, A, B}.
