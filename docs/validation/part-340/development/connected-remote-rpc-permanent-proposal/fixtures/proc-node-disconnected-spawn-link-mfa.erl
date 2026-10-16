-module(oracle_connected_dependency).
-export([run/0,child/1]).
run() ->
    process_flag(trap_exit,true),P=self(),C=proc_lib:spawn_link(missing_proc_node@localhost,oracle_connected_dependency,child,[P]),receive {'EXIT',C,noconnection}->ok end.
child(P) -> P ! {ready,self(),get('$ancestors')}, receive stop -> ok end.
joined(C,R) -> C ! stop, receive {'DOWN',R,process,C,normal} -> ok end.
