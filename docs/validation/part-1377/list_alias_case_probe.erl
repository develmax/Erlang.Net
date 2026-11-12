-module(list_alias_case_probe).
-export([scan/3, choose/2, from_proper_list/1]).

scan([], _, _) ->
    empty;
scan([Head | Tail], Context = {Tag, Mode}, Pair = {Key, Value}) ->
    case Head of
        Key -> {Tag, Context, Mode, Value};
        _ -> scan(Tail, Context, Pair)
    end.

choose([], _) ->
    empty;
choose([Head | Tail], Limit) ->
    if
        Head < Limit -> {found, Head};
        true -> choose(Tail, Limit)
    end.

from_proper_list(Context) ->
    scan_private([hit], Context).

scan_private([], _) ->
    empty;
scan_private([Head | _], Context = {Key, Value}) ->
    case Head of
        Key -> {found, Context, Value};
        _ -> missing
    end.
