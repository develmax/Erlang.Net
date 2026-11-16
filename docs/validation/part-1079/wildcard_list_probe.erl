-module(wildcard_list_probe).
-export([read/2, exact/1, tagged/2]).
read([_,Value|_], Extra) -> {Extra, erlang:put(list_key,Value)};
read([fallback], Extra) -> {fallback, Extra}.
exact([_,_]) -> {two, erlang:self()};
exact(_) -> other.

tagged([tag,42,V|T], Extra) -> {erlang:put(tag_key,V),T,Extra};
tagged(_, Extra) -> {fallback,Extra}.

