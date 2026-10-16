-module(oracle_connected_dependency).
-export([run/0,child/1]).
run() ->
    P=self(),true=register(oracle_node_parent,P),put('$ancestors',[root]),
    {C,R}=proc_lib:spawn_opt(node(),fun() -> child(P) end,[monitor]),
    receive {ready,C,[oracle_node_parent,root]} -> ok end,
    ok=joined(C,R),true=unregister(oracle_node_parent),ok.
child(P) -> P ! {ready,self(),get('$ancestors')}, receive stop -> ok end.
joined(C,R) -> C ! stop, receive {'DOWN',R,process,C,normal} -> ok end.
