-module(oracle_connected_dependency).
-export([run/0]).
run() ->
    try proc_lib:spawn_opt(node(),fun() -> ok end,invalid_options) of V -> {unexpected_return,V} catch error:badarg -> ok end.
