-module(size_observe_map_size).
-export([run/0,call/1]).
run() -> try call(bad) catch Class:Reason:Stack -> {Class,Reason,Stack} end.
call(V) -> F=fun erlang:map_size/1,F(V).
