-module(tuple_common_entry_probe).
-export([read/2, strict/2]).
read({tag,42,a,V}, Extra) when is_atom(V) ->
    {erlang:put(tuple_common_key,V),V,Extra};
read({tag,42,b,V}, Extra) ->
    {erlang:put(tuple_common_key,V),Extra,V}.
strict({tag,42,{inner,V}}, Extra) ->
    {erlang:put(tuple_strict_key,V),V,Extra}.
