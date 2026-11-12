-module(tuple_register_entry_probe).
-export([read/2]).
read({tag,42,Unused,Value}, Extra) ->
    {erlang:put(tuple_register_key,Value),Value,Extra}.
