-module(beam_ets_all420).
-export([run/0,run_stacks/0,worker/2,private_owner/1,capture/1]).
cases()->[public_named,public_anonymous,private_foreign,request_protocol,renamed,owner_dead,no_extra_reply,bad_arity].
run()->[{Name,observe(Name)}||Name<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name)->{Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name)->V=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,Parent!{Name,V}.
cleanup(T)->try ets:delete(T) catch error:badarg->ok end.
named()->ets:new(all_alpha420,[named_table,private]).
perform(public_named)->T=named(),try lists:member(T,ets:all()) after cleanup(T) end;
perform(public_anonymous)->T=ets:new(all_anon420,[private]),try {is_reference(T),lists:member(T,ets:all())} after cleanup(T) end;
perform(private_foreign)->{Pid,Ref}=spawn_monitor(?MODULE,private_owner,[self()]),receive {ready,T}->try lists:member(T,ets:all()) after Pid!stop,receive {'DOWN',Ref,process,Pid,normal}->ok end end end;
perform(request_protocol)->T=named(),U=ets:new(all_anon420,[]),try {Ref,Lists}=request(),{is_reference(Ref),lists:member(T,lists:append(Lists)),lists:member(U,lists:append(Lists)),length(Lists)=:=erlang:system_info(schedulers),lists:all(fun is_list/1,Lists)} after cleanup(T),cleanup(U) end;
perform(renamed)->T=named(),try Next=ets:rename(T,all_beta420),All=ets:all(),{lists:member(T,All),lists:member(Next,All)} after cleanup(all_beta420) end;
perform(owner_dead)->{Pid,Ref}=spawn_monitor(?MODULE,private_owner,[self()]),receive {ready,T}->Pid!stop,receive {'DOWN',Ref,process,Pid,normal}->not lists:member(T,ets:all()) end end;
perform(no_extra_reply)->{Ref,_}=request(),receive {Ref,_}->false after 0->true end;
perform(bad_arity)->erlang:apply(ets,internal_request_all,[bad]).
private_owner(Parent)->T=named(),Parent!{ready,T},receive stop->ok end.
request()->Ref=ets:internal_request_all(),{Ref,collect(Ref,erlang:system_info(schedulers),[])}.
collect(_,0,Acc)->Acc;
collect(Ref,N,Acc)->receive {Ref,List}->collect(Ref,N-1,[List|Acc]) after 10000->exit(reply_timeout) end.
capture([Dir])->ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
