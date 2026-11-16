-module(wildcard_list_probe).
-export([read/2, exact/1]).
read([_,Value|_], Extra) -> {Extra, erlang:put(list_key,Value)};
read([fallback], Extra) -> {fallback, Extra}.
exact([_,_]) -> {two, erlang:self()};
exact(_) -> other.
