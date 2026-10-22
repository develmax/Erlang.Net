-module(beam_process_alive425).
-export([run/0,run_stacks/0,worker/2,wait/0,capture/1,run_own/0]).
cases()->[self,live,dead,kill_before,tworefs,bad_integer,bad_tuple,bad_list,bad_binary,bad_atom].
run()->[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),E=erlang:system_flag(backtrace_depth,D),{D,E,run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end;{'DOWN',R,process,P,Why}->exit({worker_failed,N,Why}) after 10000->exit(P,kill),exit(worker_timeout) end.
worker(Parent,N)->V=try {value,perform(N)} catch C:R:S->{exception,C,R,S} end,Parent!{N,V}.
query(P)->R=make_ref(),ok=erts_internal:is_process_alive(P,R),receive {R,V}->V after 5000->exit(alive_reply_timeout) end.
wait()->receive stop->ok end.
perform(self)->query(self());
perform(live)->{P,R}=spawn_monitor(?MODULE,wait,[]),try query(P) after exit(P,kill),receive {'DOWN',R,process,P,_}->ok end end;
perform(dead)->{P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,_}->ok end,query(P);
perform(kill_before)->{P,R}=spawn_monitor(?MODULE,wait,[]),try exit(P,kill),query(P) after receive {'DOWN',R,process,P,_}->ok end end;
perform(tworefs)->A=make_ref(),B=make_ref(),ok=erts_internal:is_process_alive(self(),A),ok=erts_internal:is_process_alive(self(),B),receive {A,X}->receive {B,Y}->{X,Y} end end;
perform(bad_integer)->erts_internal:is_process_alive(0,0);
perform(bad_tuple)->erts_internal:is_process_alive({},0);
perform(bad_list)->erts_internal:is_process_alive([],0);
perform(bad_binary)->erts_internal:is_process_alive(<<>>,0);
perform(bad_atom)->erts_internal:is_process_alive(not_a_pid,0).
run_own()->[{N,own(N)}||N<-[integer_ref,list_ref,tuple_ref,magic_ref]].
own(N)->P=self(),R=case N of integer_ref->0;list_ref->[];tuple_ref->{};magic_ref->atomics:new(1,[]) end,Args=[P,R],Caught=try {value,erts_internal:is_process_alive(P,R)} catch C:Why:S->{exception,C,Why,S} end,Proof=case Caught of {exception,error,badarg,[{erts_internal,is_process_alive,Actual,_}|_]}->Actual=:=Args;_->false end,{Proof,Caught}.
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),ok=prim_file:write_file(D++"/own-state.etf",term_to_binary(run_own(),[{minor_version,2}])),erlang:halt(0).
