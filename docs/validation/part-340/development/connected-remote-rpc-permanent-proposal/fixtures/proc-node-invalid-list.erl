-module(oracle_connected_dependency).
-export([run/0]).
run() ->
    try proc_lib:spawn_opt(1,oracle_connected_dependency,child,1,invalid_options) of V -> {unexpected_return,V} catch error:function_clause -> ok end.
