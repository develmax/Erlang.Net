-module(tuple_guard_entry_probe).
-export([read/2]).
read({tag, V, Kept}, Extra) when is_atom(V) ->
    {erlang:put(tuple_guard_key,Kept),Kept,Extra};
read(Original, Extra) ->
    {fallback,Original,Extra}.
