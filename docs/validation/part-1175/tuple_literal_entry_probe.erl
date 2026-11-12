-module(tuple_literal_entry_probe).
-export([read/2]).
read({tag,42,{inner,yes,V},ignored}, Extra) ->
    {erlang:put(tuple_literal_key,V),V,Extra};
read(_, Extra) ->
    {fallback,Extra}.
