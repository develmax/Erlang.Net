-module(guard_first_call_probe).
-export([read/3]).
read(Key, Value, Extra) when is_atom(Key) ->
    Result = erlang:put(Key, Value),
    {Result, Extra}.
