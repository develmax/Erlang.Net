-module(frame_entry_probe).
-export([put_value/2, repeated/2, swapped/2, fanout/2, guarded_match/1]).
put_value(Key, Value) ->
    Previous = erlang:put(Key, Value),
    {Key, Value, Previous}.
repeated(Key, Value) ->
    Previous = erlang:put(Key, Value),
    {Previous, Key, Previous}.
swapped(Value, Key) ->
    Previous = erlang:put(Key, Value),
    {Previous, Key, Previous}.
fanout(A, B) ->
    Result = preserve(B, A, A),
    {Result, A}.
preserve(A, B, C) ->
    {A, B, C}.
guarded_match(Key) ->
    Previous = erlang:put(Key, Previous = value),
    {Key, Previous}.
