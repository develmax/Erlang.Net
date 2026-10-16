-module(oracle_erpc_execute).
-export([run/0]).
run()->R=make_ref(),{C,M}=spawn_monitor(erpc,execute_call,[R,erlang,self,[]]),receive {'DOWN',M,process,C,{R,return,C}}->ok end.
