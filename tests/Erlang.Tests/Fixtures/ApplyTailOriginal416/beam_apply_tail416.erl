-module(beam_apply_tail416).
-export([run/0,run_stacks/0,worker/2,capture/1,host_return/0,tail1/3,tail2/4,tail3/5,non_tail1/3]).
cases()->[ets_tail,ets_static_tail,ets_nontail,error1_tail,error2_tail,error3_tail,
 exit_tail,throw_tail,error_nontail,exit_nontail,throw_nontail,bad_module_tail,bad_function_tail,missing_tail].
run()->[{C,observe(C)}||C<-cases()].
run_stacks()->
 Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),
 try [{default,Old,run()}|[at_depth(D)||D<-[0,1,2,8]]]
 after erlang:system_flag(backtrace_depth,Old) end.
at_depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(C)->
 {Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),C]),
 receive {C,V}->receive {'DOWN',Ref,process,Pid,normal}->V end
 after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,C)->
 Outcome=try probe(C) of V->{returned,V} catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
 Parent!{C,Outcome},ok.
probe(ets_tail)->tail1(ets,first,missing_tail_table416);
probe(ets_static_tail)->static_ets(missing_tail_table416);
probe(ets_nontail)->non_tail1(ets,first,missing_tail_table416);
probe(error1_tail)->tail1(erlang,error,tail_reason416);
probe(error2_tail)->tail2(erlang,error,tail_reason416,[tail_argument416]);
probe(error3_tail)->tail3(erlang,error,tail_reason416,[tail_argument416],[{error_info,#{detail=>tail_detail416}}]);
probe(exit_tail)->tail1(erlang,exit,tail_reason416);
probe(throw_tail)->tail1(erlang,throw,tail_reason416);
probe(error_nontail)->non_tail1(erlang,error,tail_reason416);
probe(exit_nontail)->non_tail1(erlang,exit,tail_reason416);
probe(throw_nontail)->non_tail1(erlang,throw,tail_reason416);
probe(bad_module_tail)->tail1(42,first,tail_argument416);
probe(bad_function_tail)->tail1(ets,42,tail_argument416);
probe(missing_tail)->tail1(ets,missing_apply_tail416,tail_argument416).
tail1(Module,Function,Argument)->Module:Function(Argument).
tail2(Module,Function,First,Second)->Module:Function(First,Second).
tail3(Module,Function,First,Second,Third)->Module:Function(First,Second,Third).
non_tail1(Module,Function,Argument)->Value=Module:Function(Argument),{done,Value}.
static_ets(Table)->Actual=identity(Table),ets:first(Actual).
identity(Value)->erlang:put(apply_tail_marker416,Value),Value.
host_return()->tail1(erlang,error,host_replacement_reason416).
capture([Output])->
 V=run(),S=run_stacks(),
 ok=prim_file:write_file(Output++"/values.etf",term_to_binary(V,[{minor_version,2}])),
 ok=prim_file:write_file(Output++"/stacks.etf",term_to_binary(S,[{minor_version,2}])),erlang:halt(0).
