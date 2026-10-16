-module(oracle_rpc_zero).
-export([run/0,hold/1]).
run()->{badrpc,timeout}=rpc:call(node(),oracle_rpc_zero,hold,[self()],0),receive {holding,C}->M=monitor(process,C),C!stop,receive {'DOWN',M,process,C,{Result,return,ok}} when is_reference(Result)->ok end end.
hold(P)->P!{holding,self()},receive stop->ok end.
