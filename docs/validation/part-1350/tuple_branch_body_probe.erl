-module(tuple_branch_body_probe).
-export([run/1, frame/1, capture/1]).

run([Node = {make,_} | Tail]) ->
    Value = make(Node),
    finish(Tail, Value);
run([{skip,_,_} | Tail]) ->
    finish(Tail, skip);
run(Other) ->
    {rest, Other}.

frame([{value, Item} | Tail]) ->
    case convert(Item) of
        {ready, N} -> finish(Tail, N);
        Other -> {unready, Other}
    end;
frame([{combine, Left, Right, Keep} | Tail]) ->
    First = make(Left),
    Second = make(Right),
    {Keep, Tail, First, Second};
frame(Other) ->
    {rest, Other}.

capture([{small, Item} | Tail]) ->
    Value = make(Item),
    fun() -> {small, Tail, Value} end;
capture([{large, Item, Kept} | Tail]) ->
    Value = make(Item),
    fun() -> {large, Tail, Kept, Value} end;
capture(Other) ->
    {rest, Other}.

make(Value) ->
    {made, Value}.

finish(Tail, Value) ->
    {finished, Tail, Value}.

convert(Value) when is_integer(Value) ->
    {ready, Value};
convert(Value) ->
    Value.
