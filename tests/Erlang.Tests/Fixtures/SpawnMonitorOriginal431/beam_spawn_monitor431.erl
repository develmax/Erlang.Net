-module(beam_spawn_monitor431).
-export([run/0,run_stacks/0,worker/2,kernel/1,capture/1]).
cases()->[monitor,monitor_tuple,monitor_alias,opt_monitor,no_spawn,wrong_arity_one,wrong_arity_two].
run()->ensure_kernel(),[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),{D,erlang:system_flag(backtrace_depth,D),run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end;{'DOWN',R,process,P,Why}->exit({worker_failed,N,Why}) after 15000->exit(P,kill),exit(timeout) end.
worker(Parent,N)->V=try {value,perform(N)} catch C:R:S->{exception,C,R,S} end,Parent!{N,V}.
ensure_kernel()->case whereis(net_kernel) of undefined->Parent=self(),K=spawn(?MODULE,kernel,[Parent]),receive {kernel_ready,K}->ok after 5000->error(kernel_timeout) end;_->ok end.
kernel(Parent)->true=register(net_kernel,self()),true=erlang:setnode(monitor431_local@localhost,1),Parent!{kernel_ready,self()},receive stop->ok end.
flags()->(element(3,erts_internal:get_dflags()) bor (1 bsl 32)) band bnot (8 bor 32).
channel(N,F)->Node=list_to_atom("monitor431_"++atom_to_list(N)++"@localhost"),_=erts_internal:new_connection(Node),{ok,H}=erts_internal:create_dist_channel(Node,self(),{F,1}),{Node,H}.
member(_,[])->false;member(X,[X|_])->true;member(X,[_|T])->member(X,T).
data(H,0)->erlang:dist_ctrl_get_data(H);data(H,N)->case erlang:dist_ctrl_get_data(H) of none->receive after 1->data(H,N-1) end;V->V end.
probe(N,Options,Type)->{Node,H}=channel(N,flags()),Result=erts_internal:dist_spawn_request(Node,{module431,function431,[]},Options,Type),{Ref,Mon}=case Result of {R,M}->{R,M};R->{R,none} end,
 Binary=iolist_to_binary(data(H,5000)),<<112,Terms/binary>>=Binary,Control=binary_to_term(Terms),
 {is_reference(Ref),Mon,element(1,Control)=:=29,element(2,Control)=:=Ref,member(monitor,element(6,Control))}.
perform(monitor)->probe(monitor,[monitor],spawn_request);
perform(monitor_tuple)->probe(monitor_tuple,[{monitor,[]}],spawn_request);
perform(monitor_alias)->probe(monitor_alias,[{monitor,[{alias,demonitor}]}],spawn_request);
perform(opt_monitor)->probe(opt_monitor,[monitor],spawn_opt);
perform(no_spawn)->{Node,H}=channel(no_spawn,flags() band bnot (1 bsl 32)),Ref=erts_internal:dist_spawn_request(Node,{module431,function431,[]},[monitor],spawn_request),Reply=receive {spawn_reply,Ref,error,notsup}->true after 0->false end,{is_reference(Ref),Reply,erlang:dist_ctrl_get_data(H)=:=none};
perform(wrong_arity_one)->apply(erts_internal,dist_spawn_request,[bad431]);
perform(wrong_arity_two)->apply(erts_internal,dist_spawn_request,[bad431,other431]).
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
