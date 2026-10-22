-module(beam_dirty_query423).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[self_pid,waiting_pid,dead_pid,external_pid,bad_atom,bad_integer,bad_nil,bad_tuple,bad_list,bad_float,bad_binary,bad_map].
run()->[{Name,observe(Name)}||Name<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name)->{Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name)->V=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,Parent!{Name,V}.
perform(self_pid)->erts_internal:is_process_executing_dirty(self());
perform(waiting_pid)->P=self(),{Pid,Ref}=spawn_monitor(fun()->P!ready,receive stop->ok end end),receive ready->try erts_internal:is_process_executing_dirty(Pid) after Pid!stop,receive {'DOWN',Ref,process,Pid,normal}->ok end end end;
perform(dead_pid)->{Pid,Ref}=spawn_monitor(fun()->ok end),receive {'DOWN',Ref,process,Pid,normal}->erts_internal:is_process_executing_dirty(Pid) end;
perform(external_pid)->erts_internal:is_process_executing_dirty(binary_to_term(<<131,103,100,0,12,"dirty423@ext",0,0,0,1,0,0,0,0,0>>));
perform(bad_atom)->erts_internal:is_process_executing_dirty(bad);
perform(bad_integer)->erts_internal:is_process_executing_dirty(42);
perform(bad_nil)->erts_internal:is_process_executing_dirty([]);
perform(bad_tuple)->erts_internal:is_process_executing_dirty({a,b});
perform(bad_list)->erts_internal:is_process_executing_dirty([a,b]);
perform(bad_float)->erts_internal:is_process_executing_dirty(1.5);
perform(bad_binary)->erts_internal:is_process_executing_dirty(<<1,2>>);
perform(bad_map)->erts_internal:is_process_executing_dirty(#{a=>1}).
capture([Dir])->ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
