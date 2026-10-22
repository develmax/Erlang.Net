-module(beam_system_tasks428).
-export([run/0,run_stacks/0,worker/2,role_actors/0,capture/1]).
cases()->[test_low,test_normal,test_high,test_max,test_inherit,test4,
 gc_minor,gc_major,gc4_minor,gc4_major,cpc,cpc4,dead_test,dead_gc,dead_cpc,sink4,
 bad_target3,bad_target4,ordinary_check_zero,ordinary_check_atoms,
 ordinary_handle_zero,ordinary_handle_tuple,actual_roles,actual_handler_cpc].
run()->[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),E=erlang:system_flag(backtrace_depth,D),{D,E,run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end;{'DOWN',R,process,P,Why}->exit({worker_failed,N,Why}) after 15000->exit(P,kill),exit(worker_timeout) end.
worker(Parent,N)->V=try {value,perform(N)} catch C:R:S->{exception,C,R,S} end,Parent!{N,V}.
request(Prio,Op)->Ret=erts_internal:request_system_task(self(),Prio,Op),reply(Op,Ret).
request4(Prio,Op)->Ret=erts_internal:request_system_task(self(),self(),Prio,Op),reply(Op,Ret).
reply(Op,Ret)->Tag=element(1,Op),Id=element(2,Op),receive {Tag,Id,Value}->{Ret,Value} after 10000->exit(system_task_reply_timeout) end.
dead()->{P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,normal}->P end.
dead_request(Op)->Ret=erts_internal:request_system_task(dead(),normal,Op),reply(Op,Ret).
perform(test_low)->request(low,{system_task_test,request428});
perform(test_normal)->request(normal,{system_task_test,request428});
perform(test_high)->request(high,{system_task_test,request428});
perform(test_max)->request(max,{system_task_test,request428});
perform(test_inherit)->request(inherit,{system_task_test,request428});
perform(test4)->request4(normal,{system_task_test,request428,ignored,ignored_too});
perform(gc_minor)->request(normal,{garbage_collect,request428,minor});
perform(gc_major)->request(normal,{garbage_collect,request428,major});
perform(gc4_minor)->request4(normal,{garbage_collect,request428,minor,ignored});
perform(gc4_major)->request4(low,{garbage_collect,request428,major});
perform(cpc)->request(normal,{check_process_code,request428,system_task_absent428});
perform(cpc4)->request4(high,{check_process_code,request428,system_task_absent428,ignored});
perform(dead_test)->dead_request({system_task_test,request428});
perform(dead_gc)->dead_request({garbage_collect,request428,minor});
perform(dead_cpc)->dead_request({check_process_code,request428,system_task_absent428});
perform(sink4)->Parent=self(),{Sink,Ref}=spawn_monitor(fun()->receive M->Parent!{sink_reply,M} end end),
 Ret=erts_internal:request_system_task(Sink,self(),normal,{system_task_test,request428}),
 receive {sink_reply,{system_task_test,request428,Value}}->receive {'DOWN',Ref,process,Sink,normal}->{Ret,Value} end after 10000->exit(sink_timeout) end;
perform(bad_target3)->erts_internal:request_system_task(0,normal,{system_task_test,request428});
perform(bad_target4)->erts_internal:request_system_task(0,0,normal,{system_task_test,request428});
perform(ordinary_check_zero)->erts_internal:check_dirty_process_code(0,0);
perform(ordinary_check_atoms)->erts_internal:check_dirty_process_code(target428,module428);
perform(ordinary_handle_zero)->erts_internal:dirty_process_handle_signals(0);
perform(ordinary_handle_tuple)->erts_internal:dirty_process_handle_signals({});
perform(actual_roles)->Roles=role_actors(),{length(Roles),has_role(normal,Roles),has_role(high,Roles),has_role(max,Roles)};
perform(actual_handler_cpc)->{probe_handler(normal),probe_handler(high),probe_handler(max)}.
role_actors()->[{P,process_info(P,priority),process_info(P,initial_call)}||P<-processes(),process_info(P,initial_call)=:={initial_call,{erts_dirty_process_signal_handler,start,0}}].
has_role(Prio,[{_,{priority,Prio},_}|_])->true;
has_role(Prio,[_|Rest])->has_role(Prio,Rest);
has_role(_,[])->false.
find_role(Prio,[{P,{priority,Prio},_}|_])->P;
find_role(Prio,[_|Rest])->find_role(Prio,Rest);
find_role(_,[])->exit(dirty_handler_missing).
probe_handler(Prio)->Handler=find_role(Prio,role_actors()),Ref=make_ref(),Handler!{self(),self(),Prio,{check_process_code,Ref,system_task_absent428}},
 receive {check_process_code,Ref,false}->true after 10000->exit(dirty_handler_timeout) end.
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
 ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),
 ok=prim_file:write_file(D++"/role-actors.etf",term_to_binary(role_actors(),[{minor_version,2}])),erlang:halt(0).
