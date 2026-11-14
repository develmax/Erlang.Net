-module(beam_leaf_registers).
-export([unwrap/1]).
unwrap([Value]) ->
    Value;
unwrap(Other) ->
    Other.
