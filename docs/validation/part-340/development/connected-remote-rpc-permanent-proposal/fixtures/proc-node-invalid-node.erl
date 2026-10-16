-module(oracle_connected_dependency).
-export([run/0]).
run() ->
    try proc_lib:spawn(1,fun() -> ok end) of V -> {unexpected_return,V} catch error:badarg -> ok end.
