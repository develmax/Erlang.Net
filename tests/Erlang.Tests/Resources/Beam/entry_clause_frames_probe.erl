-module(entry_clause_frames_probe).
-export([read/3]).
read(a, _Unused, V) ->
    {erlang:put(entry_clause_key,V),V};
read(_, _Other, V) ->
    {fallback,erlang:put(entry_clause_key,V)}.
