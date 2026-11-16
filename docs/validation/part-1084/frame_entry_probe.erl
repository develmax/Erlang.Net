-module(frame_entry_probe).
-export([put_value/2, repeated/2, guarded_match/1]).
put_value(Key, Value) ->
    Previous = erlang:put(Key, Value),
    {Key, Value, Previous}.
repeated(Key, Value) ->
    Previous = erlang:put(Key, Value),
    {Previous, Key, Previous}.
guarded_match(Key) ->
    Previous = erlang:put(Key, Previous = value),
    {Key, Previous}.
