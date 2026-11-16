-module(shared_shape_probe).
-export([tagged/1, varying/1, fallback/1]).

tagged(T = {tag, one, Value}) -> {T, erlang:put(key, Value)};
tagged(T = {tag, two, Value}) -> {T, erlang:put(key, Value)}.
varying({left, Value}) -> {Value, erlang:put(key, left)};
varying({right, Value}) -> {Value, erlang:put(key, right)}.
fallback({tag, Value}) -> {Value, erlang:put(key, tag)};
fallback(Value) -> {fallback, Value}.
