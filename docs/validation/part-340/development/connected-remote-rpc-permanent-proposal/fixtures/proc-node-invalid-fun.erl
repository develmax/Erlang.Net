-module(oracle_connected_dependency).
-export([run/0]).
run() ->
    try proc_lib:spawn(1,invalid_fun) of V -> {unexpected_return,V} catch error:function_clause -> ok end.
