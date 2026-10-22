-module(beam_check_code423).
-export([run/2,run_stacks/2,worker/4,reply/1,probe/0,capture/1]).
-define(S,beam_check_code_subject423).
cases()->[missing,current_only,current_after_reload,old_waiting,old_non_tail,old_tail,old_tail_native,fun_only,saved_trace,bad_integer,bad_list,bad_binary,bad_tuple].
run(B1,B2)->[{N,observe(N,B1,B2)}||N<-cases()].
run_stacks(B1,B2)->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run(B1,B2)}|[depth(D,B1,B2)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D,B1,B2)->erlang:system_flag(backtrace_depth,D),E=erlang:system_flag(backtrace_depth,D),{D,E,run(B1,B2)}.
observe(N,B1,B2)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N,B1,B2]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end;{'DOWN',R,process,P,Reason}->exit({worker_failed,N,Reason}) after 10000->exit(P,kill),receive {'DOWN',R,process,P,_}->ok end,exit(worker_timeout) end.
worker(Parent,N,B1,B2)->V=try reset(),{value,perform(N,B1,B2)} catch C:R:S->{exception,C,R,S} after reset() end,Parent!{N,V}.
purge()->try erlang:purge_module(?S) catch error:badarg->false end.
reset()->purge(),erlang:delete_module(?S),purge(),ok.
load(B)->{module,?S}=erlang:load_module(?S,B),ok.
probe()->erts_internal:check_process_code(?S).
reply(P)->P!{answer,probe()},ok.
old(B1,B2,Message)->load(B1),{P,R}=spawn_monitor(?S,wait,[self()]),try receive {ready,P}->ok end,load(B2),P!{Message,self()},receive {answer,V}->V end after exit(P,kill),receive {'DOWN',R,process,P,_}->ok end end.
perform(missing,_,_)->erts_internal:check_process_code(no_such_module423);
perform(current_only,B1,_)->load(B1),?S:check();
perform(current_after_reload,B1,B2)->load(B1),load(B2),?S:check();
perform(old_waiting,B1,B2)->old(B1,B2,probe);
perform(old_non_tail,B1,B2)->old(B1,B2,retain);
perform(old_tail,B1,B2)->old(B1,B2,tail);
perform(old_tail_native,B1,B2)->load(B1),Parent=self(),{P,R}=spawn_monitor(fun()->V=?S:wait_tail_native(Parent),Parent!{answer,V}end),try receive {ready,P}->ok end,load(B2),P!go,receive {answer,V}->V end after exit(P,kill),receive {'DOWN',R,process,P,_}->ok end end;
perform(fun_only,B1,B2)->load(B1),F=?S:make_fun(),load(B2),{probe(),is_function(F,0)};
perform(saved_trace,B1,B2)->load(B1),S=?S:trace(),load(B2),{probe(),S};
perform(bad_integer,_,_)->erts_internal:check_process_code(1);
perform(bad_list,_,_)->erts_internal:check_process_code([]);
perform(bad_binary,_,_)->erts_internal:check_process_code(<<>>);
perform(bad_tuple,_,_)->erts_internal:check_process_code({a}).
capture([D])->{ok,B1}=prim_file:read_file(D++"/subject-v1/beam_check_code_subject423.beam"),{ok,B2}=prim_file:read_file(D++"/subject-v2/beam_check_code_subject423.beam"),ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(B1,B2),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(B1,B2),[{minor_version,2}])),erlang:halt(0).
