-module(tuple_nested_entry_probe).
-export([read/2]).
read({tag,42,{inner,yes,V}}, Extra) ->
    {erlang:put(tuple_nested_key,V),V,Extra}.
