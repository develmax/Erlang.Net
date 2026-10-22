-module(beam_internal_spawn429).
-export([run/0,run_stacks/0,capture/1,observer_child/3,child_target/2]).
cases() -> [bad_module,bad_function,bad_args_atom,bad_args_improper,options_atom,
 options_improper,options_bad_prefix_improper,bad_option,bad_reply,custom_error_tag,
 error_suppressed,success,custom_success_tag,monitor_down,reply_no,reply_error_only,
 reply_success_only,priority,message_queue_data,reply_last_wins,child_error].
run() -> [{C,observe(C,default)} || C <- cases()].
run_stacks() -> [{D,actual_depth(D),[{C,observe(C,D)} || C <- cases()]} || D <- [default,0,1,2,8]].
actual_depth(default) -> V=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,V),V;
actual_depth(D) -> D.
observe(C,D) ->
 {P,R}=spawn_monitor(?MODULE,observer_child,[self(),C,D]),
 receive {P,V} -> receive {'DOWN',R,process,P,normal} -> V end;
         {'DOWN',R,process,P,Why} -> {unexpected_down,Why}
 after 10000 -> exit(P,kill),erlang:error({observer_timeout,C}) end.
observer_child(Parent,C,D) ->
 case D of default -> ok; _ -> erlang:system_flag(backtrace_depth,D) end,
 V=try probe(C) of X -> {value,X} catch K:R:S -> {exception,K,R,S} end,
 Parent!{self(),V}.
probe(bad_module) -> erts_internal:spawn_request(17,child_target,[],[]);
probe(bad_function) -> erts_internal:spawn_request(?MODULE,17,[],[]);
probe(bad_args_atom) -> erts_internal:spawn_request(?MODULE,child_target,bad,[]);
probe(bad_args_improper) -> erts_internal:spawn_request(?MODULE,child_target,[1|bad],[]);
probe(options_atom) -> erts_internal:spawn_request(?MODULE,child_target,[],bad);
probe(options_improper) -> erts_internal:spawn_request(?MODULE,child_target,[],[monitor|bad]);
probe(options_bad_prefix_improper) -> erts_internal:spawn_request(?MODULE,child_target,[],[invalid|bad]);
probe(bad_option) -> failed_request([invalid],spawn_reply);
probe(bad_reply) -> failed_request([{reply,invalid}],spawn_reply);
probe(custom_error_tag) -> failed_request([invalid,{reply_tag,custom}],custom);
probe(error_suppressed) -> R=erts_internal:spawn_request(?MODULE,child_target,[],[invalid,{reply,no}]),
 {is_reference(R),receive {_,R,_,_} -> false after 0 -> true end};
probe(success) -> successful([],spawn_reply,yes,normal);
probe(custom_success_tag) -> successful([{reply_tag,custom}],custom,yes,normal);
probe(monitor_down) -> successful([monitor],spawn_reply,yes,normal);
probe(reply_no) -> successful([{reply,no}],spawn_reply,no,normal);
probe(reply_error_only) -> successful([{reply,error_only}],spawn_reply,no,normal);
probe(reply_success_only) -> successful([{reply,success_only}],spawn_reply,yes,normal);
probe(priority) -> successful([{priority,high}],spawn_reply,yes,normal);
probe(message_queue_data) -> successful([{message_queue_data,off_heap}],spawn_reply,yes,normal);
probe(reply_last_wins) -> successful([{reply,no},{reply,yes}],spawn_reply,yes,normal);
probe(child_error) -> successful([monitor],spawn_reply,yes,error).
failed_request(Options,Tag) ->
 R=erts_internal:spawn_request(?MODULE,child_target,[],Options),
 receive {Tag,R,error,Reason} -> {is_reference(R),Reason} after 5000 -> erlang:error(error_reply_timeout) end.
successful(Options,Tag,Reply,Mode) ->
 R=erts_internal:spawn_request(?MODULE,child_target,[self(),Mode],Options),
 P=case Reply of yes -> receive {Tag,R,ok,Pid} -> Pid after 5000 -> erlang:error(success_reply_timeout) end; no -> undefined end,
 receive {started,Child,Meta} ->
  Valid=case Reply of yes -> P=:=Child; no -> receive {_,R,_,_} -> false after 0 -> true end end,
  Child!release,
  Down=case monitored(Options) of
   true -> receive {'DOWN',R,process,Child,Why} -> Why after 5000 -> erlang:error(down_timeout) end;
   false -> unmonitored
  end,
  {is_reference(R),is_pid(Child),Valid,Meta,Down}
 after 5000 -> erlang:error(child_start_timeout) end.
monitored([monitor|_]) -> true;
monitored([_|Rest]) -> monitored(Rest);
monitored([]) -> false.
child_target(Parent,Mode) ->
 Parent!{started,self(),{process_info(self(),priority),process_info(self(),message_queue_data)}},
 receive release -> case Mode of normal -> ok; error -> erlang:error(child_failure429) end end.
capture([Out]) ->
 ok=file:write_file(Out++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
 ok=file:write_file(Out++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),halt().
