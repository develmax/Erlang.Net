-module(dead_clause_input_probe).
-export([read/3, preserve/3]).
read(a, Unused, V) ->
    {erlang:put(dead_clause_input,V),V};
read(_, Other, V) ->
    {fallback,erlang:put(dead_clause_input,V)}.
preserve({Key}, Key, Unused) when is_atom(Key) ->
    {matched,erlang:put(dead_clause_key,Key)};
preserve(_, Key, _) ->
    {fallback,Key}.
