-module(oracle_connected_dependency).
-export([run/0,child/1]).
run() ->
    P=self(),C=proc_lib:spawn(missing_proc_node@localhost,fun() -> child(P) end),R=monitor(process,C),receive {'DOWN',R,process,C,Reason} when Reason=:=noconnection;Reason=:=noproc -> ok end.
child(P) -> P ! {ready,self(),get('$ancestors')}, receive stop -> ok end.
joined(C,R) -> C ! stop, receive {'DOWN',R,process,C,normal} -> ok end.
