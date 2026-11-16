-module(shared_shape_probe).
-export([tagged/1, varying/1, fallback/1, singles/2]).

tagged(T = {tag, one, Value}) -> {T, erlang:put(key, Value)};
tagged(T = {tag, two, Value}) -> {T, erlang:put(key, Value)}.
varying({left, Value}) when is_integer(Value) -> {Value, erlang:put(key, left)};
varying({right, marker, Value}) -> {Value, erlang:put(key, right)};
varying({left, Value}) -> {Value, erlang:put(key, later)}.
fallback({tag, Value}) -> {Value, erlang:put(key, tag)};
fallback(Value) -> {fallback, Value}.



singles({a, Value}, Other) -> {Other, erlang:put(key, Value)};
singles({b, marker, Value}, Other) -> {Other, erlang:put(key, Value)}.

