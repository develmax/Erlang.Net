-module(tuple_shared_dispatch_probe).
-export([read/2]).
read({tag,42,a,Left,_}, Extra) when is_atom(Extra) ->
    {erlang:put(tuple_dispatch_key,Left),Left,Extra};
read({tag,42,b,_,Right}, Extra) when is_integer(Extra) ->
    {erlang:put(tuple_dispatch_key,Right),Right,Extra}.
