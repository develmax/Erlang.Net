-module(oracle_connected_dependency).
-export([run/0]).
run() ->
    Self=self(),Child=erpc:call(node(),erlang,self,[],1000),true=is_pid(Child),false=(Child=:=Self),ok.
