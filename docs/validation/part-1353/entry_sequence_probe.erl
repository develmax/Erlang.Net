-module(entry_sequence_probe).
-export([walk/2, pack/1, branch/1]).

walk([Node = {value, _} | Tail], Acc) ->
    First = box(Node),
    Second = combine(First, Acc),
    finish(Tail, Second);
walk(Other, Acc) ->
    finish(Other, Acc).

pack([Node = {value, _} | Tail]) ->
    First = box(Node),
    Second = wrap([First]),
    finish(Tail, Second);
pack(Other) ->
    rest(Other).

branch([{item, Left, Right} | Tail]) ->
    First = box(Left),
    Second = box(Right),
    {Tail, First, Second};
branch(Other) ->
    rest(Other).

box(Value) ->
    {boxed, Value}.

combine(First, Acc) ->
    {First, Acc}.

wrap(Value) ->
    {wrapped, Value}.

finish(Tail, Value) ->
    {done, Tail, Value}.

rest(Value) ->
    {rest, Value}.
