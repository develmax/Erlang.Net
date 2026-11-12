-module(capture_order_probe).
-export([walk/3, choose/1]).

walk(Left, [{selected, Item} | Tail], Right) ->
    Value = box(Item),
    fun(Argument) -> collect(Left, Argument, Tail, Right, Value) end;
walk(Left, [Item | Tail], Right) ->
    Value = box(Item),
    fun(Argument) -> collect(Left, Argument, Tail, Right, Value) end;
walk(_, [], _) ->
    empty.

choose([Node = {value, _} | Tail]) ->
    case prepare(Node) of
        {ready, Result} -> collect_case(Tail, Result);
        Other -> {unready, Other}
    end;
choose([{skip, _, _} | Tail]) ->
    collect_case(Tail, skip);
choose(Other) ->
    {rest, Other}.

box(Value) ->
    {boxed, Value}.

collect(Left, Argument, Tail, Right, Value) ->
    {Left, Argument, Tail, Right, Value}.

prepare({value, Value}) when is_integer(Value) ->
    {ready, Value};
prepare({value, Value}) ->
    Value.

collect_case(Tail, Value) ->
    {chosen, Tail, Value}.
