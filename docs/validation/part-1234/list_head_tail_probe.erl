-module(list_head_tail_probe).
-export([find/1, bare/1]).
find([{outer, {inner, _} = Found, marker} | _]) ->
    Found;
find([_ | Rest]) ->
    find(Rest);
find([]) ->
    missing.
bare([{{inner, Value}, marker} | _]) ->
    Value;
bare([_ | Rest]) ->
    bare(Rest);
bare([]) ->
    missing.
