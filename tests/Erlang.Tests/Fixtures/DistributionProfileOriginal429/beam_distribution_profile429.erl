-module(beam_distribution_profile429).
-export([run/0,run_stacks/0,worker/2,profile_record/0,capture/1]).
cases()->[shape,mandatory_nonzero,mandatory_preferred,addable_subset,rejectable_disjoint,strict_subset,wrong_arity_one,wrong_arity_two].
run()->[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),E=erlang:system_flag(backtrace_depth,D),{D,E,run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end;{'DOWN',R,process,P,Why}->exit({worker_failed,N,Why}) after 15000->exit(P,kill),exit(worker_timeout) end.
worker(Parent,N)->V=try {value,perform(N)} catch C:R:S->{exception,C,R,S} end,Parent!{N,V}.
profile_record()->erts_internal:get_dflags().
perform(shape)->P=profile_record(),{element(1,P),tuple_size(P)};
perform(mandatory_nonzero)->element(3,profile_record())>0;
perform(mandatory_preferred)->P=profile_record(),element(3,P) band element(2,P)=:=element(3,P);
perform(addable_subset)->P=profile_record(),element(4,P) band element(2,P)=:=element(4,P);
perform(rejectable_disjoint)->P=profile_record(),element(5,P) band element(3,P)=:=0;
perform(strict_subset)->P=profile_record(),element(6,P) band element(2,P)=:=element(6,P);
perform(wrong_arity_one)->apply(erts_internal,get_dflags,[bad429]);
perform(wrong_arity_two)->apply(erts_internal,get_dflags,[bad429,other429]).
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),ok=prim_file:write_file(D++"/profile-record.etf",term_to_binary(profile_record(),[{minor_version,2}])),erlang:halt(0).
