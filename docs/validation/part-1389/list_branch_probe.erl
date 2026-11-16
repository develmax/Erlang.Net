-module(list_branch_probe).
-export([scan/1, pick/1]).

scan([]) ->
    false;
scan([Head|Tail]) when is_list(Head) =:= false, is_tuple(Head) =:= false ->
    scan(Tail);
scan([Head|Tail]) ->
    case keep(Head) of
        true -> scan(Tail);
        false -> true
    end;
scan(_) ->
    true.

pick([]) ->
    empty;
pick([One]) ->
    One;
pick([Head|Tail]) ->
    case keep(Head) of
        true -> pick(Tail);
        false -> Head
    end.

keep([]) -> true;
keep({keep}) -> true;
keep(_) -> false.
