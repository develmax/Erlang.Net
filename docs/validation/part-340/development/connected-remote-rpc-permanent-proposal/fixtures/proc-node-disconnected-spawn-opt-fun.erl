-module(oracle_connected_dependency).
-export([run/0,child/1]).
run() ->
    P=self(),{C,R}=proc_lib:spawn_opt(missing_proc_node@localhost,fun() -> child(P) end,[monitor]),receive {'DOWN',R,process,C,noconnection}->ok end.
child(P) -> P ! {ready,self(),get('$ancestors')}, receive stop -> ok end.
joined(C,R) -> C ! stop, receive {'DOWN',R,process,C,normal} -> ok end.
