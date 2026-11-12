-module(beam_list_branch_probe).
-export([scan/1, pick/1]).

scan([]) ->
    false;
scan([Head|Tail]) when is_list(Head) =:= false, is_tuple(Head) =:= false ->
    scan(Tail);
scan([Head|Tail]) ->
    case lists:member(Head, [[],{keep}]) of
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
    case lists:member(Head, [[],{keep}]) of
        true -> pick(Tail);
        false -> Head
    end.
