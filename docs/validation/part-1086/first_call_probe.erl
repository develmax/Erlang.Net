-module(first_call_probe).
-export([retain/3, discard/3, bound_unused/3, match_again/3]).
retain(Value, Key, Keep) ->
    Old = erlang:put(Key, Value),
    pair(Keep, Old).
discard(Value, Key, Keep) ->
    erlang:put(Key, Value),
    pair(Keep, ok).
bound_unused(Value, Key, Keep) ->
    _Old = erlang:put(Key, Value),
    pair(Keep, ok).
match_again(Value, Key, Expected) ->
    Expected = erlang:put(Key, Value),
    pair(Expected, ok).
pair(A, B) -> {A, B}.
