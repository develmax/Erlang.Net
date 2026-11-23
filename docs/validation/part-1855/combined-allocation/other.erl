-module(other).
-export([consume/1]).
consume(List) -> erlang:garbage_collect(), List.
