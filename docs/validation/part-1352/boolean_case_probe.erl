-module(boolean_case_probe).
-export([walk/2, unproven/1]).

walk([Pair = {Key, _Payload} | Tail], Seen) ->
    case lists:keymember(Key, 1, Seen) of
        true -> walk(Tail, Seen);
        false -> walk(Tail, [Pair | Seen])
    end;
walk([], Seen) ->
    lists:reverse(Seen).

unproven([{value, Item} | _]) ->
    case identity(Item) of
        true -> yes;
        false -> no
    end;
unproven(Other) ->
    {fallback, Other}.

identity(Value) ->
    Value.
