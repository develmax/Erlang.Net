-module(beam_projection_reuse).
-export([remember/2, pack/2, later/3]).

remember(T = {tag, Value}, Key) ->
    erlang:put(Key, T),
    Old = erlang:put(last, Value),
    New = erlang:get(Key),
    {T, Old, New}.

pack(T = {tag, Value}, Key) ->
    erlang:put(Key, T),
    Next = {T, Value},
    Other = erlang:put(next, Next),
    {Other, Next}.

later(First, T = {tag, Value}, Key) ->
    Previous = erlang:put(First, T),
    _ = erlang:put(Key, T),
    Result = erlang:put(Previous, Value),
    {T, Result}.
