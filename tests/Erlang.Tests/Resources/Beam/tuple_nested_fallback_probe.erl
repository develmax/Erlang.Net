-module(tuple_nested_fallback_probe).
-export([read/2]).
read({tag,42,{inner,yes,V}}, Extra) ->
    {erlang:put(tuple_nested_fallback_key,V),V,Extra};
read(Original, Extra) ->
    {fallback,Original,Extra}.
