-module(frame_entry_probe).
-export([put_value/2]).
put_value(Key, Value) ->
    Previous = erlang:put(Key, Value),
    {Key, Value, Previous}.
