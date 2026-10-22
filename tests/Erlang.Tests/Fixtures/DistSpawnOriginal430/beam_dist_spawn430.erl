-module(beam_dist_spawn430).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[request,monitor,opt,opt_monitor,reply_no,reply_error,reply_success,reply_tag,bad_reply,bad_early_tail,bad_node,bad_mfa,bad_opts_tail,wrong_arity].
run()->[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),{D,erlang:system_flag(backtrace_depth,D),run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end;{'DOWN',R,process,P,Why}->exit({failed,N,Why}) after 10000->exit(P,kill),exit(timeout) end.
worker(Parent,N)->V=try {value,perform(N)} catch C:R:S->{exception,C,R,S} end,Parent!{N,V}.
call(Opts,Kind)->erts_internal:dist_spawn_request(remote430@localhost,{module430,function430,[]},Opts,Kind).
protocol(Opts,Kind)->Result=call(Opts,Kind),{Ref,Monitor}=case Result of {R,M}->{R,M};R->{R,none} end,Reply=receive {Tag,Ref,error,Reason}->{Tag,Reason} after 0->none end,{is_reference(Ref),Monitor,Reply}.
perform(request)->protocol([],spawn_request);
perform(monitor)->protocol([monitor],spawn_request);
perform(opt)->protocol([],spawn_opt);
perform(opt_monitor)->protocol([monitor],spawn_opt);
perform(reply_no)->protocol([{reply,no}],spawn_request);
perform(reply_error)->protocol([{reply,error_only}],spawn_request);
perform(reply_success)->protocol([{reply,success_only}],spawn_request);
perform(reply_tag)->protocol([{reply_tag,tag430}],spawn_request);
perform(bad_reply)->protocol([{reply,bad430}],spawn_request);
perform(bad_early_tail)->protocol([{reply_tag,tag430},{reply,bad430}|tail430],spawn_request);
perform(bad_node)->erts_internal:dist_spawn_request(1,{module430,function430,[]},[],spawn_request);
perform(bad_mfa)->erts_internal:dist_spawn_request(remote430@localhost,{module430,function430,[a|b]},[],spawn_request);
perform(bad_opts_tail)->call([a|tail430],spawn_request);
perform(wrong_arity)->apply(erts_internal,dist_spawn_request,[bad430]).
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
