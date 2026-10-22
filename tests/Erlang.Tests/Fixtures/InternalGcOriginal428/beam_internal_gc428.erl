-module(beam_internal_gc428).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[minor,major,bad_integer,bad_list,bad_boolean,bad_tuple].
run()->[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),E=erlang:system_flag(backtrace_depth,D),{D,E,run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end;{'DOWN',R,process,P,Why}->exit({worker_failed,N,Why}) after 10000->exit(P,kill),exit(worker_timeout) end.
worker(Parent,N)->V=try {value,perform(N)} catch C:R:S->{exception,C,R,S} end,Parent!{N,V}.
perform(minor)->erts_internal:garbage_collect(minor);
perform(major)->erts_internal:garbage_collect(major);
perform(bad_integer)->erts_internal:garbage_collect(0);
perform(bad_list)->erts_internal:garbage_collect([]);
perform(bad_boolean)->erts_internal:garbage_collect(true);
perform(bad_tuple)->erts_internal:garbage_collect({}).
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
