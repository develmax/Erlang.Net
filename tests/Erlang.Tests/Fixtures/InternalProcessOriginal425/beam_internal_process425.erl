-module(beam_internal_process425).
-export([run/0,run_stacks/0,capture/1,child/3,probe/1,held/0]).

cases() -> [leader_self,leader_signal,leader_twice,leader_bad,leader_bad_target,
 leader3_bad,display_self,display_remote,display_badopt,display_badpid,
 flag_self,flag_remote,flag_badtype,flag_badflag,flag_badvalue,suspend_self,
 suspend_badpid,suspend_badopt,suspend_async,monitor_get,monitor_clear,
 monitor_bad_session,monitor_bad_list,monitor_disable,monitor_options].

run() -> [{C,observe(C,default)} || C <- cases()].
run_stacks() -> [{D,actual_depth(D),[{C,observe(C,D)} || C <- cases()]} || D <- [default,0,1,2,8]].
actual_depth(default) -> D=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,D),D;
actual_depth(D) -> D.
observe(C,D) ->
 {P,R}=spawn_monitor(?MODULE,child,[self(),C,D]),
 receive {P,V} -> receive {'DOWN',R,process,P,normal} -> V end;
         {'DOWN',R,process,P,Why} -> {unexpected_down,Why}
 after 10000 -> exit(P,kill),erlang:error({observer_timeout,C}) end.
child(Parent,C,D) ->
 case D of default -> ok; _ -> erlang:system_flag(backtrace_depth,D) end,
 V=try probe(C) of X -> {value,X} catch K:R:S -> {exception,K,R,S} end,
 Parent ! {self(),V}.
await(R) when is_reference(R) -> receive {R,V} -> V after 5000 -> erlang:error(native_reply_timeout) end;
await(V) -> V.
held() -> receive stop -> ok end.
with_target(F) -> P=spawn(?MODULE,held,[]),try F(P) after exit(P,kill) end.
probe(leader_self) -> erts_internal:group_leader(self(),self());
probe(leader_signal) -> R=make_ref(),ok=erts_internal:group_leader(self(),self(),R),await(R);
probe(leader_twice) -> R=make_ref(),ok=erts_internal:group_leader(self(),self(),R),ok=erts_internal:group_leader(self(),self(),R),{await(R),await(R)};
probe(leader_bad) -> erts_internal:group_leader(bad,bad);
probe(leader_bad_target) -> erts_internal:group_leader(self(),bad);
probe(leader3_bad) -> erts_internal:group_leader(bad,bad,bad);
probe(display_self) -> erts_internal:process_display(self(),backtrace);
probe(display_remote) -> with_target(fun(P) -> await(erts_internal:process_display(P,backtrace)) end);
probe(display_badopt) -> erts_internal:process_display(bad,bad);
probe(display_badpid) -> erts_internal:process_display(bad,backtrace);
probe(flag_self) -> {erts_internal:process_flag(self(),save_calls,4),erts_internal:process_flag(self(),save_calls,0)};
probe(flag_remote) -> with_target(fun(P) -> {await(erts_internal:process_flag(P,save_calls,4)),await(erts_internal:process_flag(P,save_calls,0))} end);
probe(flag_badtype) -> erts_internal:process_flag(bad,save_calls,0);
probe(flag_badflag) -> erts_internal:process_flag(self(),not_a_flag,0);
probe(flag_badvalue) -> erts_internal:process_flag(self(),save_calls,-1);
probe(suspend_self) -> erts_internal:suspend_process(self(),[]);
probe(suspend_badpid) -> erts_internal:suspend_process(bad,[]);
probe(suspend_badopt) -> with_target(fun(P) -> erts_internal:suspend_process(P,[bad]) end);
probe(suspend_async) -> with_target(fun(P) -> R=make_ref(),V=erts_internal:suspend_process(P,[{asynchronous,R}]),{V,await(R),erlang:resume_process(P)} end);
probe(monitor_get) -> erts_internal:system_monitor(legacy);
probe(monitor_clear) -> erts_internal:system_monitor(legacy,undefined,[]);
probe(monitor_bad_session) -> erts_internal:system_monitor(bad);
probe(monitor_bad_list) -> erts_internal:system_monitor(legacy,undefined,bad);
probe(monitor_disable) -> erts_internal:system_monitor(legacy,undefined,[bad|bad]);
probe(monitor_options) ->
 Prev=erts_internal:system_monitor(legacy,self(),[{long_message_queue,{1,4}},busy_port]),
 {Pid,Opts}=erts_internal:system_monitor(legacy),true=Pid=:=self(),
 _=erts_internal:system_monitor(legacy,undefined,[]), {Prev,Opts}.
capture([Out]) ->
 ok=file:write_file(Out++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
 ok=file:write_file(Out++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),halt().
