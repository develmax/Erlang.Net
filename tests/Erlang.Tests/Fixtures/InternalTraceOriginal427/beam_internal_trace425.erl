-module(beam_internal_trace425).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[legacy_none,legacy_pattern_send,legacy_pattern_receive,create_shape,destroy_twice,destroy_weak,session_flags,session_send_enabled,session_isolation,duplicate_names,bad_create_name,bad_create_opts,bad_create_tracer,bad_destroy,bad_trace_session,bad_pattern_session,bad_info_session,bad_trace_flags,bad_trace_how,bad_pattern_flags,bad_info_any_key].
run()->[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),E=erlang:system_flag(backtrace_depth,D),{D,E,run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end;{'DOWN',R,process,P,Why}->exit({worker_failed,N,Why}) after 10000->exit(P,kill),exit(worker_timeout) end.
worker(Parent,N)->V=try {value,perform(N)} catch C:R:S->{exception,C,R,S} end,Parent!{N,V}.
new()->erts_internal:trace_session_create(beam_trace425,undefined,[]).
with_session(F)->S=new(),try F(S) after erts_internal:trace_session_destroy(S) end.
perform(legacy_none)->erts_internal:trace(self(),true,[]);
perform(legacy_pattern_send)->try erts_internal:trace_pattern(send,true,[]) after erts_internal:trace_pattern(send,true,[]) end;
perform(legacy_pattern_receive)->try erts_internal:trace_pattern('receive',false,[]) after erts_internal:trace_pattern('receive',true,[]) end;
perform(create_shape)->with_session(fun({Strong,{Name,Id}})->is_reference(Strong) andalso Name=:=beam_trace425 andalso is_integer(Id) end);
perform(destroy_twice)->S=new(),{erts_internal:trace_session_destroy(S),erts_internal:trace_session_destroy(S)};
perform(destroy_weak)->S=new(),{erts_internal:trace_session_destroy(element(2,S)),erts_internal:trace_session_destroy(S)};
perform(session_flags)->with_session(fun(S)->erts_internal:trace_info(S,self(),flags) end);
perform(session_send_enabled)->with_session(fun(S)->1=erts_internal:trace_pattern(S,send,false,[]),erts_internal:trace_info(S,send,match_spec) end);
perform(session_isolation)->with_session(fun(A)->with_session(fun(B)->1=erts_internal:trace(A,self(),true,[send]),{erts_internal:trace_info(A,self(),flags),erts_internal:trace_info(B,self(),flags)} end) end);
perform(duplicate_names)->with_session(fun(A)->with_session(fun(B)->element(2,A)=/=element(2,B) end) end);
perform(bad_create_name)->erts_internal:trace_session_create(0,undefined,[]);
perform(bad_create_opts)->erts_internal:trace_session_create(beam_trace425,undefined,0);
perform(bad_create_tracer)->erts_internal:trace_session_create(beam_trace425,0,[]);
perform(bad_destroy)->erts_internal:trace_session_destroy(0);
perform(bad_trace_session)->erts_internal:trace(0,0,true,[]);
perform(bad_pattern_session)->erts_internal:trace_pattern(0,send,true,[]);
perform(bad_info_session)->erts_internal:trace_info(0,send,match_spec);
perform(bad_trace_flags)->erts_internal:trace(0,true,[bad_trace425]);
perform(bad_trace_how)->erts_internal:trace(0,0,[]);
perform(bad_pattern_flags)->erts_internal:trace_pattern(send,true,[bad_trace425]);
perform(bad_info_any_key)->erts_internal:trace_info(any,any,flags).
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
