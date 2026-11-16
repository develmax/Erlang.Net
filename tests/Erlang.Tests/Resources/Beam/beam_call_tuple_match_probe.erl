-module(beam_call_tuple_match_probe).
-export([year/0, known/2, nested/2, unknown/2]).

year() ->
    {Y,_,_} = date(),
    integer_to_list(Y).

known(A, B) ->
    {Head,Tail} = parts(A, B),
    finish(Tail, [Head]).

nested(Key, Pairs) ->
    {Tag,_} = parts(Key, Pairs),
    lists:keydelete(wrap(Tag), 1, Pairs).

unknown(Key, Pairs) ->
    {A,B} = proplists:get_value(Key, Pairs),
    finish(A, B).

parts(A, B) -> {A,B}.
finish(A, B) -> {A,B}.
wrap(A) -> A.
