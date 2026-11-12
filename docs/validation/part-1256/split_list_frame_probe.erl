-module(split_list_frame_probe).
-export([read/2, walk/2]).

read([Head|Tail], Key) when is_map(Head) ->
    {maps:get(Key, Head), Tail};
read([], _) ->
    empty;
read(_, _) ->
    invalid.

walk([Head|Tail], _) when is_atom(Head) ->
    walk(Tail, Head);
walk([], Acc) ->
    Acc;
walk(_, _) ->
    invalid.
